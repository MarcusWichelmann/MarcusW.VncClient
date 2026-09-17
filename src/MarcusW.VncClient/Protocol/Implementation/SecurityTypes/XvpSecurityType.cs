using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Security;
using MarcusW.VncClient.Utils;

namespace MarcusW.VncClient.Protocol.Implementation.SecurityTypes
{
    /// <summary>
    /// A security type that implements Colin Dean's XVP (eXtended VNC Protocol) authentication.
    /// This extends basic VNC authentication with additional virtual machine management capabilities.
    /// </summary>
    public class XvpSecurityType : ISecurityType
    {
        private readonly RfbConnectionContext _context;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownSecurityType.XVP;

        /// <inheritdoc />
        public string Name => "XVP";

        /// <inheritdoc />
        public int Priority => 20; // Higher than basic VNC auth due to extended capabilities

        /// <summary>
        /// Initializes a new instance of the <see cref="XvpSecurityType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public XvpSecurityType(RfbConnectionContext context)
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

            // XVP authentication is similar to VNC authentication but with additional context
            // Step 1: Read challenge from server (16 bytes, same as VNC auth)
            ReadOnlyMemory<byte> challengeBytes = await transport.Stream.ReadAllAsync(16, cancellationToken).ConfigureAwait(false);

            // Step 2: Get credentials from authentication handler
            // XVP typically requires username and password
            CredentialsAuthenticationInput input = await authenticationHandler
                .ProvideAuthenticationInputAsync(_context.Connection, this, new CredentialsAuthenticationInputRequest()).ConfigureAwait(false);

            // Step 3: Create XVP-style response
            // XVP uses a combination of username and password in the challenge response
            ReadOnlyMemory<byte> response = CreateXvpResponse(challengeBytes, input.Username, input.Password);

            // Step 4: Send response to server
            await transport.Stream.WriteAsync(response, cancellationToken).ConfigureAwait(false);

            return new AuthenticationResult();
        }

        /// <inheritdoc />
        public Task ReadServerInitExtensionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <summary>
        /// Creates an XVP authentication response from the challenge, username, and password.
        /// XVP extends the VNC authentication by incorporating both username and password.
        /// </summary>
        /// <param name="challenge">The challenge received from the server.</param>
        /// <param name="username">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        /// <returns>The authentication response to send to the server.</returns>
        private static ReadOnlyMemory<byte> CreateXvpResponse(ReadOnlyMemory<byte> challenge, string username, string password)
        {
            // XVP authentication combines username and password into a single credential string
            // Format: "username:password" or sometimes just password if username is empty
            string credentials = string.IsNullOrEmpty(username) ? password : $"{username}:{password}";

            // Ensure credentials are not longer than the maximum DES key length
            // Truncate or pad as necessary (XVP typically uses first 8 characters)
            if (credentials.Length > 8)
                credentials = credentials.Substring(0, 8);

            // Convert credentials to bytes
            byte[] credentialBytes = Encoding.UTF8.GetBytes(credentials);

            // Create DES key from credentials (same method as VNC but with combined credentials)
            var key = new byte[8];
            credentialBytes.AsSpan().Slice(0, Math.Min(key.Length, credentialBytes.Length)).CopyTo(key);

            // Reverse bit order of all bytes in key (same as VNC authentication)
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

            // Encrypt challenge with the modified key using DES
            using var desProvider = new System.Security.Cryptography.DESCryptoServiceProvider 
            { 
                Key = key, 
                Mode = System.Security.Cryptography.CipherMode.ECB 
            };
            using var encryptor = desProvider.CreateEncryptor();

            // Encrypt challenge
            var response = new byte[16];
            encryptor.TransformBlock(challenge.ToArray(), 0, challenge.Length, response, 0);

            // Clear sensitive data
            Array.Clear(key, 0, key.Length);
            Array.Clear(credentialBytes, 0, credentialBytes.Length);

            return response;
        }
    }
}
