using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Security;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace MarcusW.VncClient.Protocol.Implementation.SecurityTypes
{
    /// <summary>
    /// A security type that implements RA2 (RSA-AES) authentication according to the full RFB specification.
    /// </summary>
    /// <remarks>
    /// The RA2 security type provides robust authentication and encryption using:
    /// - Bidirectional RSA key exchange for mutual authentication
    /// - Random number exchange to derive session keys
    /// - AES-EAX encryption for all subsequent protocol messages
    /// - Hash verification to prevent man-in-the-middle attacks
    /// </remarks>
    public class Ra2SecurityType : ISecurityType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<Ra2SecurityType> _logger;
        private readonly bool _useSha256;

        /// <inheritdoc />
        public byte Id { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public int Priority => _useSha256 ? 90 : 70;

        /// <summary>
        /// Initializes a new instance of the <see cref="Ra2SecurityType"/> for RA2 (SHA1).
        /// </summary>
        /// <param name="context">The connection context.</param>
        public Ra2SecurityType(RfbConnectionContext context) : this(context, false, (byte)WellKnownSecurityType.RA2, "RA2")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ra2SecurityType"/> with configurable hash algorithm.
        /// </summary>
        /// <param name="context">The connection context.</param>
        /// <param name="useSha256">True to use SHA256 (RA2_256), false to use SHA1 (RA2).</param>
        /// <param name="id">The security type ID.</param>
        /// <param name="name">The security type name.</param>
        protected Ra2SecurityType(RfbConnectionContext context, bool useSha256, byte id, string name)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<Ra2SecurityType>();
            _useSha256 = useSha256;
            Id = id;
            Name = name;
        }

        /// <inheritdoc />
        public async Task<AuthenticationResult> AuthenticateAsync(IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken = default)
        {
            if (authenticationHandler == null)
                throw new ArgumentNullException(nameof(authenticationHandler));

            ITransport transport = _context.Transport ?? throw new InvalidOperationException("Cannot access transport for authentication.");
            Stream stream = transport.Stream;

            // Step 1: Read server's RSA public key
            _logger.LogDebug("Reading server's RSA public key");
            (RSA serverRsa, byte[] serverPublicKeyBytes) = await ReadRsaPublicKeyAsync(stream, cancellationToken).ConfigureAwait(false);

            // Step 2: Generate and send client's RSA public key
            _logger.LogDebug("Generating and sending client's RSA public key");
            using RSA clientRsa = RSA.Create(2048);
            byte[] clientPublicKeyBytes = await SendRsaPublicKeyAsync(stream, clientRsa, cancellationToken).ConfigureAwait(false);

            // Step 3: Read server's encrypted random number
            _logger.LogDebug("Reading server's encrypted random");
            byte[] serverRandom = await ReadEncryptedRandomAsync(stream, clientRsa, cancellationToken).ConfigureAwait(false);

            // Step 4: Generate and send client's encrypted random number
            _logger.LogDebug("Generating and sending client's encrypted random");
            byte[] clientRandom = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(clientRandom);

            await SendEncryptedRandomAsync(stream, serverRsa, clientRandom, cancellationToken).ConfigureAwait(false);

            // Step 5: Derive session keys
            _logger.LogDebug("Deriving AES session keys");
            (byte[] clientSessionKey, byte[] serverSessionKey) = DeriveSessionKeys(serverRandom, clientRandom);

            // Step 6: Switch to AES-EAX encrypted transport
            var encryptedTransport = new AesEaxTransport(transport, clientSessionKey, serverSessionKey);
            Stream encryptedStream = encryptedTransport.Stream;

            // Step 7: Verify server hash
            _logger.LogDebug("Verifying server hash");
            await VerifyServerHashAsync(encryptedStream, serverPublicKeyBytes, clientPublicKeyBytes, cancellationToken).ConfigureAwait(false);

            // Step 8: Send client hash
            _logger.LogDebug("Sending client hash");
            await SendClientHashAsync(encryptedStream, clientPublicKeyBytes, serverPublicKeyBytes, cancellationToken).ConfigureAwait(false);

            // Step 9: Read subtype
            _logger.LogDebug("Reading authentication subtype");
            byte subtype = await ReadSubtypeAsync(encryptedStream, cancellationToken).ConfigureAwait(false);

            // Step 10: Send credentials
            _logger.LogDebug("Sending credentials");
            CredentialsAuthenticationInput input = await authenticationHandler
                .ProvideAuthenticationInputAsync(_context.Connection, this, new CredentialsAuthenticationInputRequest()).ConfigureAwait(false);

            await SendCredentialsAsync(encryptedStream, subtype, input.Username, input.Password, cancellationToken).ConfigureAwait(false);

            // Clean up sensitive data
            Array.Clear(serverRandom, 0, serverRandom.Length);
            Array.Clear(clientRandom, 0, clientRandom.Length);
            Array.Clear(clientSessionKey, 0, clientSessionKey.Length);
            Array.Clear(serverSessionKey, 0, serverSessionKey.Length);
            serverRsa.Dispose();

            _logger.LogInformation("RA2 authentication handshake completed");

            // Return the encrypted transport - all subsequent messages will be encrypted
            return new AuthenticationResult(encryptedTransport, expectSecurityResult: true);
        }

        /// <inheritdoc />
        public Task ReadServerInitExtensionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private async Task<(RSA rsa, byte[] publicKeyBytes)> ReadRsaPublicKeyAsync(Stream stream, CancellationToken cancellationToken)
        {
            // Read key length (4 bytes, big-endian)
            byte[] keyLengthBuffer = new byte[4];
            await stream.ReadExactlyAsync(keyLengthBuffer, cancellationToken).ConfigureAwait(false);
            uint keyLengthBits = BinaryPrimitives.ReadUInt32BigEndian(keyLengthBuffer);

            if (keyLengthBits < 1024 || keyLengthBits > 8192)
                throw new InvalidOperationException($"Invalid RSA key length: {keyLengthBits} bits");

            int keyLengthBytes = (int)Math.Ceiling(keyLengthBits / 8.0);

            // Read modulus
            byte[] modulus = new byte[keyLengthBytes];
            await stream.ReadExactlyAsync(modulus, cancellationToken).ConfigureAwait(false);

            // Read public exponent
            byte[] exponent = new byte[keyLengthBytes];
            await stream.ReadExactlyAsync(exponent, cancellationToken).ConfigureAwait(false);

            // Build the public key bytes for hash verification (as sent by server)
            byte[] publicKeyBytes = new byte[4 + keyLengthBytes * 2];
            BinaryPrimitives.WriteUInt32BigEndian(publicKeyBytes, keyLengthBits);
            modulus.CopyTo(publicKeyBytes, 4);
            exponent.CopyTo(publicKeyBytes, 4 + keyLengthBytes);

            // Import RSA parameters
            var rsa = RSA.Create();
            var rsaParams = new RSAParameters
            {
                Modulus = modulus,
                Exponent = TrimLeadingZeros(exponent)
            };
            rsa.ImportParameters(rsaParams);

            return (rsa, publicKeyBytes);
        }

        private async Task<byte[]> SendRsaPublicKeyAsync(Stream stream, RSA rsa, CancellationToken cancellationToken)
        {
            RSAParameters parameters = rsa.ExportParameters(false);
            byte[] modulus = parameters.Modulus!;
            byte[] exponent = PadToLength(parameters.Exponent!, modulus.Length);

            uint keyLengthBits = (uint)(modulus.Length * 8);

            // Build public key bytes
            byte[] publicKeyBytes = new byte[4 + modulus.Length * 2];
            BinaryPrimitives.WriteUInt32BigEndian(publicKeyBytes, keyLengthBits);
            modulus.CopyTo(publicKeyBytes, 4);
            exponent.CopyTo(publicKeyBytes, 4 + modulus.Length);

            // Send to server
            await stream.WriteAsync(publicKeyBytes, cancellationToken).ConfigureAwait(false);

            return publicKeyBytes;
        }

        private async Task<byte[]> ReadEncryptedRandomAsync(Stream stream, RSA clientRsa, CancellationToken cancellationToken)
        {
            // Read length (2 bytes, big-endian)
            byte[] lengthBuffer = new byte[2];
            await stream.ReadExactlyAsync(lengthBuffer, cancellationToken).ConfigureAwait(false);
            ushort length = BinaryPrimitives.ReadUInt16BigEndian(lengthBuffer);

            // Read encrypted random
            byte[] encryptedRandom = new byte[length];
            await stream.ReadExactlyAsync(encryptedRandom, cancellationToken).ConfigureAwait(false);

            // Decrypt with client's private key
            byte[] serverRandom = clientRsa.Decrypt(encryptedRandom, RSAEncryptionPadding.Pkcs1);

            if (serverRandom.Length != 16)
                throw new InvalidOperationException($"Server random must be 16 bytes, got {serverRandom.Length}");

            return serverRandom;
        }

        private async Task SendEncryptedRandomAsync(Stream stream, RSA serverRsa, byte[] clientRandom, CancellationToken cancellationToken)
        {
            // Encrypt client random with server's public key
            byte[] encryptedRandom = serverRsa.Encrypt(clientRandom, RSAEncryptionPadding.Pkcs1);

            // Send length (2 bytes, big-endian)
            byte[] lengthBuffer = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(lengthBuffer, (ushort)encryptedRandom.Length);
            await stream.WriteAsync(lengthBuffer, cancellationToken).ConfigureAwait(false);

            // Send encrypted random
            await stream.WriteAsync(encryptedRandom, cancellationToken).ConfigureAwait(false);
        }

        private (byte[] clientKey, byte[] serverKey) DeriveSessionKeys(byte[] serverRandom, byte[] clientRandom)
        {
            _logger.LogDebug("Deriving session keys");
            
            // Debug logging - uncomment if needed for troubleshooting
            //var msg = $"[RA2] Server random: {BitConverter.ToString(serverRandom).Replace("-", " ")}";
            //Console.WriteLine(msg);
            //System.Diagnostics.Debug.WriteLine(msg);
            //msg = $"[RA2] Client random: {BitConverter.ToString(clientRandom).Replace("-", " ")}";
            //Console.WriteLine(msg);
            //System.Diagnostics.Debug.WriteLine(msg);

            // Concatenate randoms
            byte[] serverClientConcat = new byte[32];
            byte[] clientServerConcat = new byte[32];
            serverRandom.CopyTo(serverClientConcat, 0);
            clientRandom.CopyTo(serverClientConcat, 16);
            clientRandom.CopyTo(clientServerConcat, 0);
            serverRandom.CopyTo(clientServerConcat, 16);

            // Debug logging - uncomment if needed for troubleshooting
            //var msg = $"[RA2] For client key: {BitConverter.ToString(serverClientConcat).Replace("-", " ")}";
            //Console.WriteLine(msg);
            //System.Diagnostics.Debug.WriteLine(msg);
            //msg = $"[RA2] For server key: {BitConverter.ToString(clientServerConcat).Replace("-", " ")}";
            //Console.WriteLine(msg);
            //System.Diagnostics.Debug.WriteLine(msg);

            // Derive keys using appropriate hash algorithm
            byte[] clientSessionKey;
            byte[] serverSessionKey;

            if (_useSha256)
            {
                using var sha256 = SHA256.Create();
                byte[] clientHash = sha256.ComputeHash(serverClientConcat);
                byte[] serverHash = sha256.ComputeHash(clientServerConcat);
                clientSessionKey = clientHash[..16]; // First 16 bytes
                serverSessionKey = serverHash[..16];
            }
            else
            {
                using var sha1 = SHA1.Create();
                byte[] clientHash = sha1.ComputeHash(serverClientConcat);
                byte[] serverHash = sha1.ComputeHash(clientServerConcat);
                clientSessionKey = clientHash[..16]; // First 16 bytes
                serverSessionKey = serverHash[..16];
            }

            // Debug logging - uncomment if needed for troubleshooting
            //var msg = $"[RA2] Client session key: {BitConverter.ToString(clientSessionKey).Replace("-", " ")}";
            //Console.WriteLine(msg);
            //System.Diagnostics.Debug.WriteLine(msg);
            //msg = $"[RA2] Server session key: {BitConverter.ToString(serverSessionKey).Replace("-", " ")}";
            //Console.WriteLine(msg);
            //System.Diagnostics.Debug.WriteLine(msg);

            Array.Clear(serverClientConcat, 0, serverClientConcat.Length);
            Array.Clear(clientServerConcat, 0, clientServerConcat.Length);

            return (clientSessionKey, serverSessionKey);
        }

        private async Task VerifyServerHashAsync(Stream encryptedStream, byte[] serverPublicKey, byte[] clientPublicKey, CancellationToken cancellationToken)
        {
            // Read encrypted server hash (should be 20 or 32 bytes depending on hash algorithm)
            int hashSize = _useSha256 ? 32 : 20;
            byte[] encryptedServerHash = new byte[hashSize];
            await encryptedStream.ReadExactlyAsync(encryptedServerHash, cancellationToken).ConfigureAwait(false);

            // Compute expected server hash: SHA(ServerPublicKey || ClientPublicKey)
            byte[] combined = new byte[serverPublicKey.Length + clientPublicKey.Length];
            serverPublicKey.CopyTo(combined, 0);
            clientPublicKey.CopyTo(combined, serverPublicKey.Length);

            byte[] expectedServerHash;
            if (_useSha256)
            {
                using var sha256 = SHA256.Create();
                expectedServerHash = sha256.ComputeHash(combined);
            }
            else
            {
                using var sha1 = SHA1.Create();
                expectedServerHash = sha1.ComputeHash(combined);
            }

            // Verify
            if (!AreEqual(encryptedServerHash, expectedServerHash))
                throw new AuthenticationException("Server hash verification failed - possible man-in-the-middle attack");
        }

        private async Task SendClientHashAsync(Stream encryptedStream, byte[] clientPublicKey, byte[] serverPublicKey, CancellationToken cancellationToken)
        {
            // Compute client hash: SHA(ClientPublicKey || ServerPublicKey)
            byte[] combined = new byte[clientPublicKey.Length + serverPublicKey.Length];
            clientPublicKey.CopyTo(combined, 0);
            serverPublicKey.CopyTo(combined, clientPublicKey.Length);

            byte[] clientHash;
            if (_useSha256)
            {
                using var sha256 = SHA256.Create();
                clientHash = sha256.ComputeHash(combined);
            }
            else
            {
                using var sha1 = SHA1.Create();
                clientHash = sha1.ComputeHash(combined);
            }

            // Send encrypted client hash
            await encryptedStream.WriteAsync(clientHash, cancellationToken).ConfigureAwait(false);
        }

        private async Task<byte> ReadSubtypeAsync(Stream encryptedStream, CancellationToken cancellationToken)
        {
            byte[] subtypeBuffer = new byte[1];
            await encryptedStream.ReadExactlyAsync(subtypeBuffer, cancellationToken).ConfigureAwait(false);
            return subtypeBuffer[0];
        }

        private async Task SendCredentialsAsync(Stream encryptedStream, byte subtype, string username, string password, CancellationToken cancellationToken)
        {
            byte[] credentialsMessage;

            if (subtype == 1) // Username + Password
            {
                byte[] usernameBytes = Encoding.UTF8.GetBytes(username ?? string.Empty);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);

                if (usernameBytes.Length > 255)
                    throw new ArgumentException("Username too long (max 255 bytes UTF-8)", nameof(username));
                if (passwordBytes.Length > 255)
                    throw new ArgumentException("Password too long (max 255 bytes UTF-8)", nameof(password));

                credentialsMessage = new byte[1 + usernameBytes.Length + 1 + passwordBytes.Length];
                int offset = 0;
                credentialsMessage[offset++] = (byte)usernameBytes.Length;
                usernameBytes.CopyTo(credentialsMessage, offset);
                offset += usernameBytes.Length;
                credentialsMessage[offset++] = (byte)passwordBytes.Length;
                passwordBytes.CopyTo(credentialsMessage, offset);

                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
            else if (subtype == 2) // Password only
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);

                if (passwordBytes.Length > 255)
                    throw new ArgumentException("Password too long (max 255 bytes UTF-8)", nameof(password));

                credentialsMessage = new byte[1 + passwordBytes.Length];
                credentialsMessage[0] = (byte)passwordBytes.Length;
                passwordBytes.CopyTo(credentialsMessage, 1);

                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
            else
            {
                throw new InvalidOperationException($"Invalid RA2 subtype: {subtype}");
            }

            // Send encrypted credentials
            await encryptedStream.WriteAsync(credentialsMessage, cancellationToken).ConfigureAwait(false);

            Array.Clear(credentialsMessage, 0, credentialsMessage.Length);
        }

        private static byte[] TrimLeadingZeros(byte[] data)
        {
            int firstNonZero = 0;
            while (firstNonZero < data.Length && data[firstNonZero] == 0)
                firstNonZero++;

            if (firstNonZero == 0)
                return data;

            byte[] trimmed = new byte[data.Length - firstNonZero];
            Array.Copy(data, firstNonZero, trimmed, 0, trimmed.Length);
            return trimmed;
        }

        private static byte[] PadToLength(byte[] data, int length)
        {
            if (data.Length == length)
                return data;

            byte[] padded = new byte[length];
            int padding = length - data.Length;
            Array.Copy(data, 0, padded, padding, data.Length);
            return padded;
        }

        private static bool AreEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];

            return result == 0;
        }
    }

    /// <summary>
    /// A security type that implements RA2ne (RSA-AES without transport encryption) authentication.
    /// </summary>
    /// <remarks>
    /// RA2ne follows the same authentication protocol as RA2, including RSA key exchange,
    /// random number exchange, and hash verification. However, after the security handshake
    /// completes, subsequent protocol messages are NOT encrypted (only the handshake itself uses encryption).
    /// </remarks>
    public class Ra2neSecurityType : ISecurityType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<Ra2neSecurityType> _logger;
        private readonly bool _useSha256;

        /// <inheritdoc />
        public byte Id { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public int Priority => _useSha256 ? 60 : 50;

        /// <summary>
        /// Initializes a new instance of the <see cref="Ra2neSecurityType"/> for RA2ne (SHA1).
        /// </summary>
        /// <param name="context">The connection context.</param>
        public Ra2neSecurityType(RfbConnectionContext context) : this(context, false, (byte)WellKnownSecurityType.RA2ne, "RA2ne")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ra2neSecurityType"/> with configurable hash algorithm.
        /// </summary>
        /// <param name="context">The connection context.</param>
        /// <param name="useSha256">True to use SHA256 (RA2ne_256), false to use SHA1 (RA2ne).</param>
        /// <param name="id">The security type ID.</param>
        /// <param name="name">The security type name.</param>
        protected Ra2neSecurityType(RfbConnectionContext context, bool useSha256, byte id, string name)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<Ra2neSecurityType>();
            _useSha256 = useSha256;
            Id = id;
            Name = name;
        }

        /// <inheritdoc />
        public async Task<AuthenticationResult> AuthenticateAsync(IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken = default)
        {
            if (authenticationHandler == null)
                throw new ArgumentNullException(nameof(authenticationHandler));

            ITransport transport = _context.Transport ?? throw new InvalidOperationException("Cannot access transport for authentication.");
            Stream stream = transport.Stream;

            // RA2ne follows the same protocol as RA2 but doesn't encrypt subsequent messages
            // Step 1: Read server's RSA public key
            _logger.LogDebug("Reading server's RSA public key");
            (RSA serverRsa, byte[] serverPublicKeyBytes) = await ReadRsaPublicKeyAsync(stream, cancellationToken).ConfigureAwait(false);

            // Step 2: Generate and send client's RSA public key
            _logger.LogDebug("Generating and sending client's RSA public key");
            using RSA clientRsa = RSA.Create(2048);
            byte[] clientPublicKeyBytes = await SendRsaPublicKeyAsync(stream, clientRsa, cancellationToken).ConfigureAwait(false);

            // Step 3: Read server's encrypted random number
            _logger.LogDebug("Reading server's encrypted random");
            byte[] serverRandom = await ReadEncryptedRandomAsync(stream, clientRsa, cancellationToken).ConfigureAwait(false);

            // Step 4: Generate and send client's encrypted random number
            _logger.LogDebug("Generating and sending client's encrypted random");
            byte[] clientRandom = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(clientRandom);

            await SendEncryptedRandomAsync(stream, serverRsa, clientRandom, cancellationToken).ConfigureAwait(false);

            // Step 5: Derive session keys (used only for handshake verification)
            _logger.LogDebug("Deriving session keys for handshake verification");
            (byte[] clientSessionKey, byte[] serverSessionKey) = DeriveSessionKeys(serverRandom, clientRandom);

            // Step 6: Create temporary encrypted transport for handshake verification
            var tempEncryptedTransport = new AesEaxTransport(transport, clientSessionKey, serverSessionKey);
            Stream encryptedStream = tempEncryptedTransport.Stream;

            // Step 7: Verify server hash
            _logger.LogDebug("Verifying server hash");
            await VerifyServerHashAsync(encryptedStream, serverPublicKeyBytes, clientPublicKeyBytes, cancellationToken).ConfigureAwait(false);

            // Step 8: Send client hash
            _logger.LogDebug("Sending client hash");
            await SendClientHashAsync(encryptedStream, clientPublicKeyBytes, serverPublicKeyBytes, cancellationToken).ConfigureAwait(false);

            // Step 9: Read subtype
            _logger.LogDebug("Reading authentication subtype");
            byte subtype = await ReadSubtypeAsync(encryptedStream, cancellationToken).ConfigureAwait(false);

            // Step 10: Send credentials
            _logger.LogDebug("Sending credentials");
            CredentialsAuthenticationInput input = await authenticationHandler
                .ProvideAuthenticationInputAsync(_context.Connection, this, new CredentialsAuthenticationInputRequest()).ConfigureAwait(false);

            await SendCredentialsAsync(encryptedStream, subtype, input.Username, input.Password, cancellationToken).ConfigureAwait(false);

            // Clean up sensitive data
            Array.Clear(serverRandom, 0, serverRandom.Length);
            Array.Clear(clientRandom, 0, clientRandom.Length);
            Array.Clear(clientSessionKey, 0, clientSessionKey.Length);
            Array.Clear(serverSessionKey, 0, serverSessionKey.Length);
            serverRsa.Dispose();

            _logger.LogInformation("RA2ne authentication handshake completed (no transport encryption)");

            // Return null tunnel transport - subsequent messages are NOT encrypted
            return new AuthenticationResult(tunnelTransport: null, expectSecurityResult: true);
        }

        /// <inheritdoc />
        public Task ReadServerInitExtensionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        // Shared helper methods (same as Ra2SecurityType)
        private async Task<(RSA rsa, byte[] publicKeyBytes)> ReadRsaPublicKeyAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] keyLengthBuffer = new byte[4];
            await stream.ReadExactlyAsync(keyLengthBuffer, cancellationToken).ConfigureAwait(false);
            uint keyLengthBits = BinaryPrimitives.ReadUInt32BigEndian(keyLengthBuffer);

            if (keyLengthBits < 1024 || keyLengthBits > 8192)
                throw new InvalidOperationException($"Invalid RSA key length: {keyLengthBits} bits");

            int keyLengthBytes = (int)Math.Ceiling(keyLengthBits / 8.0);

            byte[] modulus = new byte[keyLengthBytes];
            await stream.ReadExactlyAsync(modulus, cancellationToken).ConfigureAwait(false);

            byte[] exponent = new byte[keyLengthBytes];
            await stream.ReadExactlyAsync(exponent, cancellationToken).ConfigureAwait(false);

            byte[] publicKeyBytes = new byte[4 + keyLengthBytes * 2];
            BinaryPrimitives.WriteUInt32BigEndian(publicKeyBytes, keyLengthBits);
            modulus.CopyTo(publicKeyBytes, 4);
            exponent.CopyTo(publicKeyBytes, 4 + keyLengthBytes);

            var rsa = RSA.Create();
            var rsaParams = new RSAParameters
            {
                Modulus = modulus,
                Exponent = TrimLeadingZeros(exponent)
            };
            rsa.ImportParameters(rsaParams);

            return (rsa, publicKeyBytes);
        }

        private async Task<byte[]> SendRsaPublicKeyAsync(Stream stream, RSA rsa, CancellationToken cancellationToken)
        {
            RSAParameters parameters = rsa.ExportParameters(false);
            byte[] modulus = parameters.Modulus!;
            byte[] exponent = PadToLength(parameters.Exponent!, modulus.Length);

            uint keyLengthBits = (uint)(modulus.Length * 8);

            byte[] publicKeyBytes = new byte[4 + modulus.Length * 2];
            BinaryPrimitives.WriteUInt32BigEndian(publicKeyBytes, keyLengthBits);
            modulus.CopyTo(publicKeyBytes, 4);
            exponent.CopyTo(publicKeyBytes, 4 + modulus.Length);

            await stream.WriteAsync(publicKeyBytes, cancellationToken).ConfigureAwait(false);

            return publicKeyBytes;
        }

        private async Task<byte[]> ReadEncryptedRandomAsync(Stream stream, RSA clientRsa, CancellationToken cancellationToken)
        {
            byte[] lengthBuffer = new byte[2];
            await stream.ReadExactlyAsync(lengthBuffer, cancellationToken).ConfigureAwait(false);
            ushort length = BinaryPrimitives.ReadUInt16BigEndian(lengthBuffer);

            byte[] encryptedRandom = new byte[length];
            await stream.ReadExactlyAsync(encryptedRandom, cancellationToken).ConfigureAwait(false);

            byte[] serverRandom = clientRsa.Decrypt(encryptedRandom, RSAEncryptionPadding.Pkcs1);

            if (serverRandom.Length != 16)
                throw new InvalidOperationException($"Server random must be 16 bytes, got {serverRandom.Length}");

            return serverRandom;
        }

        private async Task SendEncryptedRandomAsync(Stream stream, RSA serverRsa, byte[] clientRandom, CancellationToken cancellationToken)
        {
            byte[] encryptedRandom = serverRsa.Encrypt(clientRandom, RSAEncryptionPadding.Pkcs1);

            byte[] lengthBuffer = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(lengthBuffer, (ushort)encryptedRandom.Length);
            await stream.WriteAsync(lengthBuffer, cancellationToken).ConfigureAwait(false);

            await stream.WriteAsync(encryptedRandom, cancellationToken).ConfigureAwait(false);
        }

        private (byte[] clientKey, byte[] serverKey) DeriveSessionKeys(byte[] serverRandom, byte[] clientRandom)
        {
            byte[] serverClientConcat = new byte[32];
            byte[] clientServerConcat = new byte[32];
            serverRandom.CopyTo(serverClientConcat, 0);
            clientRandom.CopyTo(serverClientConcat, 16);
            clientRandom.CopyTo(clientServerConcat, 0);
            serverRandom.CopyTo(clientServerConcat, 16);

            byte[] clientSessionKey;
            byte[] serverSessionKey;

            if (_useSha256)
            {
                using var sha256 = SHA256.Create();
                byte[] clientHash = sha256.ComputeHash(serverClientConcat);
                byte[] serverHash = sha256.ComputeHash(clientServerConcat);
                clientSessionKey = clientHash[..16];
                serverSessionKey = serverHash[..16];
            }
            else
            {
                using var sha1 = SHA1.Create();
                byte[] clientHash = sha1.ComputeHash(serverClientConcat);
                byte[] serverHash = sha1.ComputeHash(clientServerConcat);
                clientSessionKey = clientHash[..16];
                serverSessionKey = serverHash[..16];
            }

            Array.Clear(serverClientConcat, 0, serverClientConcat.Length);
            Array.Clear(clientServerConcat, 0, clientServerConcat.Length);

            return (clientSessionKey, serverSessionKey);
        }

        private async Task VerifyServerHashAsync(Stream encryptedStream, byte[] serverPublicKey, byte[] clientPublicKey, CancellationToken cancellationToken)
        {
            // Read encrypted server hash (should be 20 or 32 bytes depending on hash algorithm)
            int hashSize = _useSha256 ? 32 : 20;
            byte[] encryptedServerHash = new byte[hashSize];
            await encryptedStream.ReadExactlyAsync(encryptedServerHash, cancellationToken).ConfigureAwait(false);

            // Compute expected server hash: SHA(ServerPublicKey || ClientPublicKey)
            byte[] combined = new byte[serverPublicKey.Length + clientPublicKey.Length];
            serverPublicKey.CopyTo(combined, 0);
            clientPublicKey.CopyTo(combined, serverPublicKey.Length);

            byte[] expectedServerHash;
            if (_useSha256)
            {
                using var sha256 = SHA256.Create();
                expectedServerHash = sha256.ComputeHash(combined);
            }
            else
            {
                using var sha1 = SHA1.Create();
                expectedServerHash = sha1.ComputeHash(combined);
            }

            // Verify
            if (!AreEqual(encryptedServerHash, expectedServerHash))
                throw new AuthenticationException("Server hash verification failed - possible man-in-the-middle attack");
        }

        private async Task SendClientHashAsync(Stream encryptedStream, byte[] clientPublicKey, byte[] serverPublicKey, CancellationToken cancellationToken)
        {
            // Compute client hash: SHA(ClientPublicKey || ServerPublicKey)
            byte[] combined = new byte[clientPublicKey.Length + serverPublicKey.Length];
            clientPublicKey.CopyTo(combined, 0);
            serverPublicKey.CopyTo(combined, clientPublicKey.Length);

            byte[] clientHash;
            if (_useSha256)
            {
                using var sha256 = SHA256.Create();
                clientHash = sha256.ComputeHash(combined);
            }
            else
            {
                using var sha1 = SHA1.Create();
                clientHash = sha1.ComputeHash(combined);
            }

            // Send encrypted client hash
            await encryptedStream.WriteAsync(clientHash, cancellationToken).ConfigureAwait(false);
        }

        private async Task<byte> ReadSubtypeAsync(Stream encryptedStream, CancellationToken cancellationToken)
        {
            byte[] subtypeBuffer = new byte[1];
            await encryptedStream.ReadExactlyAsync(subtypeBuffer, cancellationToken).ConfigureAwait(false);
            return subtypeBuffer[0];
        }

        private async Task SendCredentialsAsync(Stream encryptedStream, byte subtype, string username, string password, CancellationToken cancellationToken)
        {
            byte[] credentialsMessage;

            if (subtype == 1)
            {
                byte[] usernameBytes = Encoding.UTF8.GetBytes(username ?? string.Empty);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);

                if (usernameBytes.Length > 255)
                    throw new ArgumentException("Username too long (max 255 bytes UTF-8)", nameof(username));
                if (passwordBytes.Length > 255)
                    throw new ArgumentException("Password too long (max 255 bytes UTF-8)", nameof(password));

                credentialsMessage = new byte[1 + usernameBytes.Length + 1 + passwordBytes.Length];
                int offset = 0;
                credentialsMessage[offset++] = (byte)usernameBytes.Length;
                usernameBytes.CopyTo(credentialsMessage, offset);
                offset += usernameBytes.Length;
                credentialsMessage[offset++] = (byte)passwordBytes.Length;
                passwordBytes.CopyTo(credentialsMessage, offset);

                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
            else if (subtype == 2)
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);

                if (passwordBytes.Length > 255)
                    throw new ArgumentException("Password too long (max 255 bytes UTF-8)", nameof(password));

                credentialsMessage = new byte[1 + passwordBytes.Length];
                credentialsMessage[0] = (byte)passwordBytes.Length;
                passwordBytes.CopyTo(credentialsMessage, 1);

                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
            else
            {
                throw new InvalidOperationException($"Invalid RA2ne subtype: {subtype}");
            }

            await encryptedStream.WriteAsync(credentialsMessage, cancellationToken).ConfigureAwait(false);

            Array.Clear(credentialsMessage, 0, credentialsMessage.Length);
        }

        private static byte[] TrimLeadingZeros(byte[] data)
        {
            int firstNonZero = 0;
            while (firstNonZero < data.Length && data[firstNonZero] == 0)
                firstNonZero++;

            if (firstNonZero == 0)
                return data;

            byte[] trimmed = new byte[data.Length - firstNonZero];
            Array.Copy(data, firstNonZero, trimmed, 0, trimmed.Length);
            return trimmed;
        }

        private static byte[] PadToLength(byte[] data, int length)
        {
            if (data.Length == length)
                return data;

            byte[] padded = new byte[length];
            int padding = length - data.Length;
            Array.Copy(data, 0, padded, padding, data.Length);
            return padded;
        }

        private static bool AreEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];

            return result == 0;
        }
    }

    /// <summary>
    /// RSA-AES-256 security type (uses SHA256 instead of SHA1).
    /// </summary>
    public class Ra2_256SecurityType : Ra2SecurityType
    {
        public Ra2_256SecurityType(RfbConnectionContext context)
            : base(context, true, (byte)WellKnownSecurityType.RA2_256, "RA2-256")
        {
        }
    }

    /// <summary>
    /// RSA-AES-256 unencrypted security type (uses SHA256 instead of SHA1).
    /// </summary>
    public class Ra2ne_256SecurityType : Ra2neSecurityType
    {
        public Ra2ne_256SecurityType(RfbConnectionContext context)
            : base(context, true, (byte)WellKnownSecurityType.RA2ne_256, "RA2ne-256")
        {
        }
    }
}
