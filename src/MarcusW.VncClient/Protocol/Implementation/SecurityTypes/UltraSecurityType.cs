using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Security;
using MarcusW.VncClient.Utils;

namespace MarcusW.VncClient.Protocol.Implementation.SecurityTypes
{
    /// <summary>
    /// A security type that implements UltraVNC authentication with enhanced security features.
    /// This provides improved password hashing and additional security mechanisms over standard VNC.
    /// </summary>
    public class UltraSecurityType : ISecurityType
    {
        private readonly RfbConnectionContext _context;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownSecurityType.Ultra;

        /// <inheritdoc />
        public string Name => "Ultra";

        /// <inheritdoc />
        public int Priority => 30; // Higher than basic VNC auth due to enhanced security

        /// <summary>
        /// Initializes a new instance of the <see cref="UltraSecurityType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public UltraSecurityType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public async Task<AuthenticationResult> AuthenticateAsync(IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken = default)
        {
            if (authenticationHandler == null)
                throw new ArgumentNullException(nameof(authenticationHandler));

            cancellationToken.ThrowIfCancellationRequested();

            ITransport transport = _context.Transport ?? throw new InvalidOperationException("Cannot access transport for authentication.");

            // Step 1: Read the authentication mode from server (1 byte)
            var modeBuffer = new byte[1];
            await transport.Stream.ReadExactlyAsync(modeBuffer, cancellationToken).ConfigureAwait(false);
            byte authMode = modeBuffer[0];

            // Step 2: Handle authentication based on mode
            switch (authMode)
            {
                case 0: // Standard Ultra authentication
                    await PerformStandardUltraAuthenticationAsync(transport.Stream, authenticationHandler, cancellationToken).ConfigureAwait(false);
                    break;

                case 1: // Enhanced Ultra authentication (with MS Logon)
                    await PerformEnhancedUltraAuthenticationAsync(transport.Stream, authenticationHandler, cancellationToken).ConfigureAwait(false);
                    break;

                case 2: // Ultra authentication with viewer challenge
                    await PerformViewerChallengeAuthenticationAsync(transport.Stream, authenticationHandler, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    throw new NotSupportedException($"Ultra authentication mode {authMode} is not supported.");
            }

            return new AuthenticationResult();
        }

        /// <inheritdoc />
        public Task ReadServerInitExtensionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <summary>
        /// Performs standard Ultra authentication.
        /// </summary>
        /// <param name="stream">The transport stream.</param>
        /// <param name="authenticationHandler">The authentication handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task PerformStandardUltraAuthenticationAsync(Stream stream, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            // Read challenge from server (16 bytes)
            var challengeBuffer = new byte[16];
            await stream.ReadExactlyAsync(challengeBuffer, cancellationToken).ConfigureAwait(false);

            // Get password from authentication handler
            PasswordAuthenticationInput input = await authenticationHandler
                .ProvideAuthenticationInputAsync(_context.Connection, this, new PasswordAuthenticationInputRequest()).ConfigureAwait(false);

            // Create Ultra-style response (enhanced DES with additional security)
            byte[] response = CreateUltraResponse(challengeBuffer, input.Password);

            // Send response
            await stream.WriteAsync(response, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs enhanced Ultra authentication with MS Logon support.
        /// </summary>
        /// <param name="stream">The transport stream.</param>
        /// <param name="authenticationHandler">The authentication handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task PerformEnhancedUltraAuthenticationAsync(Stream stream, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            // Read challenge from server (16 bytes)
            var challengeBuffer = new byte[16];
            await stream.ReadExactlyAsync(challengeBuffer, cancellationToken).ConfigureAwait(false);

            // Get credentials (username and password required for MS Logon)
            CredentialsAuthenticationInput input = await authenticationHandler
                .ProvideAuthenticationInputAsync(_context.Connection, this, new CredentialsAuthenticationInputRequest()).ConfigureAwait(false);

            // Create enhanced response with username and password
            byte[] response = CreateEnhancedUltraResponse(challengeBuffer, input.Username, input.Password);

            // Send response length (2 bytes)
            var lengthBuffer = new byte[2];
            lengthBuffer[0] = (byte)(response.Length >> 8);
            lengthBuffer[1] = (byte)(response.Length & 0xFF);
            await stream.WriteAsync(lengthBuffer, cancellationToken).ConfigureAwait(false);

            // Send response
            await stream.WriteAsync(response, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs Ultra authentication with viewer challenge (bidirectional authentication).
        /// </summary>
        /// <param name="stream">The transport stream.</param>
        /// <param name="authenticationHandler">The authentication handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task PerformViewerChallengeAuthenticationAsync(Stream stream, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            // Step 1: Read server challenge (16 bytes)
            var serverChallengeBuffer = new byte[16];
            await stream.ReadExactlyAsync(serverChallengeBuffer, cancellationToken).ConfigureAwait(false);

            // Step 2: Generate viewer challenge (16 bytes)
            var viewerChallenge = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(viewerChallenge);
            }

            // Step 3: Send viewer challenge to server
            await stream.WriteAsync(viewerChallenge, cancellationToken).ConfigureAwait(false);

            // Step 4: Get password
            PasswordAuthenticationInput input = await authenticationHandler
                .ProvideAuthenticationInputAsync(_context.Connection, this, new PasswordAuthenticationInputRequest()).ConfigureAwait(false);

            // Step 5: Create response to server challenge
            byte[] serverResponse = CreateUltraResponse(serverChallengeBuffer, input.Password);
            await stream.WriteAsync(serverResponse, cancellationToken).ConfigureAwait(false);

            // Step 6: Read server's response to viewer challenge (16 bytes)
            var serverViewerResponseBuffer = new byte[16];
            await stream.ReadExactlyAsync(serverViewerResponseBuffer, cancellationToken).ConfigureAwait(false);

            // Step 7: Verify server's response
            byte[] expectedServerResponse = CreateUltraResponse(viewerChallenge, input.Password);
            if (!ConstantTimeEquals(serverViewerResponseBuffer, expectedServerResponse))
            {
                throw new InvalidOperationException("Server failed viewer challenge authentication - server may be compromised.");
            }
        }

        /// <summary>
        /// Creates an Ultra-style authentication response with enhanced security.
        /// </summary>
        /// <param name="challenge">The challenge from the server.</param>
        /// <param name="password">The password.</param>
        /// <returns>The authentication response.</returns>
        private static byte[] CreateUltraResponse(byte[] challenge, string password)
        {
            // Ultra uses a modified DES approach with additional security
            var key = new byte[8];
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);

            // Use password bytes, but with additional hashing for security
            using var sha1 = SHA1.Create();
            byte[] hashedPassword = sha1.ComputeHash(passwordBytes);
            
            // Use first 8 bytes of hashed password as key
            Array.Copy(hashedPassword, key, Math.Min(key.Length, hashedPassword.Length));

            // Apply bit reversal (Ultra VNC requirement)
            for (var i = 0; i < key.Length; i++)
            {
                byte value = key[i];
                byte newValue = 0;
                for (var offset = 0; offset < 8; offset++)
                {
                    if ((value & (0b1 << offset)) != 0)
                        newValue |= (byte)(0b10000000 >> offset);
                }
                key[i] = newValue;
            }

            // Encrypt challenge using DES
            using var desProvider = new System.Security.Cryptography.DESCryptoServiceProvider 
            { 
                Key = key, 
                Mode = System.Security.Cryptography.CipherMode.ECB 
            };
            using var encryptor = desProvider.CreateEncryptor();

            var response = new byte[16];
            encryptor.TransformBlock(challenge, 0, challenge.Length, response, 0);

            // Clear sensitive data
            Array.Clear(key, 0, key.Length);
            Array.Clear(passwordBytes, 0, passwordBytes.Length);
            Array.Clear(hashedPassword, 0, hashedPassword.Length);

            return response;
        }

        /// <summary>
        /// Creates an enhanced Ultra authentication response with username and password.
        /// </summary>
        /// <param name="challenge">The challenge from the server.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>The enhanced authentication response.</returns>
        private static byte[] CreateEnhancedUltraResponse(byte[] challenge, string username, string password)
        {
            // Enhanced Ultra combines username and password for authentication
            string combinedCredentials = $"{username ?? string.Empty}:{password ?? string.Empty}";
            
            // Hash the combined credentials for additional security
            using var sha256 = SHA256.Create();
            byte[] credentialHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedCredentials));

            // Create DES key from credential hash
            var key = new byte[8];
            Array.Copy(credentialHash, key, Math.Min(key.Length, credentialHash.Length));

            // Apply Ultra's bit reversal
            for (var i = 0; i < key.Length; i++)
            {
                byte value = key[i];
                byte newValue = 0;
                for (var offset = 0; offset < 8; offset++)
                {
                    if ((value & (0b1 << offset)) != 0)
                        newValue |= (byte)(0b10000000 >> offset);
                }
                key[i] = newValue;
            }

            // Encrypt challenge
            using var desProvider = new System.Security.Cryptography.DESCryptoServiceProvider 
            { 
                Key = key, 
                Mode = System.Security.Cryptography.CipherMode.ECB 
            };
            using var encryptor = desProvider.CreateEncryptor();

            // Create response with additional data
            var response = new byte[32]; // Extended response for enhanced mode
            encryptor.TransformBlock(challenge, 0, challenge.Length, response, 0);

            // Add username hash to second part of response
            Array.Copy(credentialHash, 0, response, 16, 16);

            // Clear sensitive data
            Array.Clear(key, 0, key.Length);
            Array.Clear(credentialHash, 0, credentialHash.Length);

            return response;
        }

        /// <summary>
        /// Performs constant-time comparison of two byte arrays to prevent timing attacks.
        /// </summary>
        /// <param name="a">First byte array.</param>
        /// <param name="b">Second byte array.</param>
        /// <returns>True if arrays are equal, false otherwise.</returns>
        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }
    }
}
