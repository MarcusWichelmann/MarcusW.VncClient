using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Security;
using MarcusW.VncClient.Utils;

namespace MarcusW.VncClient.Protocol.Implementation.SecurityTypes
{
    /// <summary>
    /// A security type that implements Tight authentication with additional tunneling capabilities.
    /// This is used primarily by TightVNC and supports multiple sub-authentication methods.
    /// </summary>
    public class TightSecurityType : ISecurityType
    {
        private readonly RfbConnectionContext _context;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownSecurityType.Tight;

        /// <inheritdoc />
        public string Name => "Tight";

        /// <inheritdoc />
        public int Priority => 40; // Moderate priority, provides additional features

        /// <summary>
        /// Initializes a new instance of the <see cref="TightSecurityType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public TightSecurityType(RfbConnectionContext context)
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

            // Step 1: Read number of supported tunneling types
            var tunnelCountBuffer = new byte[4];
            await transport.Stream.ReadExactlyAsync(tunnelCountBuffer, cancellationToken).ConfigureAwait(false);
            uint tunnelCount = (uint)((tunnelCountBuffer[0] << 24) | (tunnelCountBuffer[1] << 16) | (tunnelCountBuffer[2] << 8) | tunnelCountBuffer[3]);

            // Step 2: Read supported tunneling types
            var tunnelTypes = new List<uint>();
            for (int i = 0; i < tunnelCount; i++)
            {
                var tunnelTypeBuffer = new byte[4];
                await transport.Stream.ReadExactlyAsync(tunnelTypeBuffer, cancellationToken).ConfigureAwait(false);
                uint tunnelType = (uint)((tunnelTypeBuffer[0] << 24) | (tunnelTypeBuffer[1] << 16) | (tunnelTypeBuffer[2] << 8) | tunnelTypeBuffer[3]);
                tunnelTypes.Add(tunnelType);
            }

            // Step 3: Choose preferred tunneling type (0 = no tunneling)
            uint chosenTunnelType = ChoosePreferredTunnelType(tunnelTypes);

            // Step 4: Send chosen tunneling type
            var chosenTunnelBuffer = new byte[4];
            chosenTunnelBuffer[0] = (byte)(chosenTunnelType >> 24);
            chosenTunnelBuffer[1] = (byte)(chosenTunnelType >> 16);
            chosenTunnelBuffer[2] = (byte)(chosenTunnelType >> 8);
            chosenTunnelBuffer[3] = (byte)(chosenTunnelType & 0xFF);
            await transport.Stream.WriteAsync(chosenTunnelBuffer, cancellationToken).ConfigureAwait(false);

            // Step 5: Handle tunneling if required
            if (chosenTunnelType != 0)
            {
                // For now, we'll implement basic support
                // In a full implementation, you would handle specific tunnel types
                throw new NotSupportedException($"Tunneling type {chosenTunnelType} is not yet supported.");
            }

            // Step 6: Read number of supported authentication types
            var authCountBuffer = new byte[4];
            await transport.Stream.ReadExactlyAsync(authCountBuffer, cancellationToken).ConfigureAwait(false);
            uint authCount = (uint)((authCountBuffer[0] << 24) | (authCountBuffer[1] << 16) | (authCountBuffer[2] << 8) | authCountBuffer[3]);

            // Step 7: Read supported authentication types
            var authTypes = new List<uint>();
            for (int i = 0; i < authCount; i++)
            {
                var authTypeBuffer = new byte[4];
                await transport.Stream.ReadExactlyAsync(authTypeBuffer, cancellationToken).ConfigureAwait(false);
                uint authType = (uint)((authTypeBuffer[0] << 24) | (authTypeBuffer[1] << 16) | (authTypeBuffer[2] << 8) | authTypeBuffer[3]);
                authTypes.Add(authType);
            }

            // Step 8: Choose preferred authentication type
            uint chosenAuthType = ChoosePreferredAuthType(authTypes);
            if (chosenAuthType == 0)
                throw new InvalidOperationException("No supported Tight authentication type found.");

            // Step 9: Send chosen authentication type
            var chosenAuthBuffer = new byte[4];
            chosenAuthBuffer[0] = (byte)(chosenAuthType >> 24);
            chosenAuthBuffer[1] = (byte)(chosenAuthType >> 16);
            chosenAuthBuffer[2] = (byte)(chosenAuthType >> 8);
            chosenAuthBuffer[3] = (byte)(chosenAuthType & 0xFF);
            await transport.Stream.WriteAsync(chosenAuthBuffer, cancellationToken).ConfigureAwait(false);

            // Step 10: Perform authentication based on chosen type
            await PerformTightAuthenticationAsync(transport.Stream, chosenAuthType, authenticationHandler, cancellationToken).ConfigureAwait(false);

            return new AuthenticationResult();
        }

        /// <inheritdoc />
        public Task ReadServerInitExtensionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <summary>
        /// Chooses the preferred tunneling type from available options.
        /// </summary>
        /// <param name="tunnelTypes">Available tunneling types.</param>
        /// <returns>The chosen tunneling type (0 for no tunneling).</returns>
        private static uint ChoosePreferredTunnelType(List<uint> tunnelTypes)
        {
            // Prefer no tunneling for simplicity (type 0)
            // In a full implementation, you might prefer encrypted tunnels
            return tunnelTypes.Contains(0u) ? 0u : tunnelTypes.FirstOrDefault();
        }

        /// <summary>
        /// Chooses the preferred authentication type from available options.
        /// </summary>
        /// <param name="authTypes">Available authentication types.</param>
        /// <returns>The chosen authentication type.</returns>
        private static uint ChoosePreferredAuthType(List<uint> authTypes)
        {
            // Tight authentication type codes:
            // 1 = None
            // 2 = VNC authentication
            // 16 = Tight authentication
            // 129 = Unix login authentication

            // Prefer in this order: Tight > VNC > Unix > None
            uint[] preferredOrder = { 16, 2, 129, 1 };
            
            foreach (uint preferred in preferredOrder)
            {
                if (authTypes.Contains(preferred))
                    return preferred;
            }

            return 0; // No supported type found
        }

        /// <summary>
        /// Performs Tight authentication based on the chosen authentication type.
        /// </summary>
        /// <param name="stream">The transport stream.</param>
        /// <param name="authType">The chosen authentication type.</param>
        /// <param name="authenticationHandler">The authentication handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task PerformTightAuthenticationAsync(Stream stream, uint authType, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            switch (authType)
            {
                case 1: // None
                    // No authentication required
                    break;

                case 2: // VNC authentication
                    await PerformVncAuthenticationAsync(stream, authenticationHandler, cancellationToken).ConfigureAwait(false);
                    break;

                case 16: // Tight authentication
                    await PerformTightSpecificAuthenticationAsync(stream, authenticationHandler, cancellationToken).ConfigureAwait(false);
                    break;

                case 129: // Unix login authentication
                    await PerformUnixLoginAuthenticationAsync(stream, authenticationHandler, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    throw new NotSupportedException($"Tight authentication type {authType} is not supported.");
            }
        }

        /// <summary>
        /// Performs VNC-style authentication within Tight security.
        /// </summary>
        /// <param name="stream">The transport stream.</param>
        /// <param name="authenticationHandler">The authentication handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task PerformVncAuthenticationAsync(Stream stream, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            // This is similar to standard VNC authentication
            // Read challenge (16 bytes)
            var challengeBuffer = new byte[16];
            await stream.ReadExactlyAsync(challengeBuffer, cancellationToken).ConfigureAwait(false);

            // Get password
            PasswordAuthenticationInput input = await authenticationHandler
                .ProvideAuthenticationInputAsync(_context.Connection, this, new PasswordAuthenticationInputRequest()).ConfigureAwait(false);

            // Create DES response (same as VNC auth)
            byte[] response = CreateVncStyleResponse(challengeBuffer, input.Password);

            // Send response
            await stream.WriteAsync(response, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs Tight-specific authentication.
        /// </summary>
        /// <param name="stream">The transport stream.</param>
        /// <param name="authenticationHandler">The authentication handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task PerformTightSpecificAuthenticationAsync(Stream stream, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            // Tight-specific authentication typically involves username and password
            CredentialsAuthenticationInput input = await authenticationHandler
                .ProvideAuthenticationInputAsync(_context.Connection, this, new CredentialsAuthenticationInputRequest()).ConfigureAwait(false);

            // Send username length and username
            byte[] usernameBytes = Encoding.UTF8.GetBytes(input.Username ?? string.Empty);
            await stream.WriteAsync(new[] { (byte)usernameBytes.Length }, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(usernameBytes, cancellationToken).ConfigureAwait(false);

            // Send password length and password
            byte[] passwordBytes = Encoding.UTF8.GetBytes(input.Password ?? string.Empty);
            await stream.WriteAsync(new[] { (byte)passwordBytes.Length }, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(passwordBytes, cancellationToken).ConfigureAwait(false);

            // Clear sensitive data
            Array.Clear(passwordBytes, 0, passwordBytes.Length);
        }

        /// <summary>
        /// Performs Unix login authentication.
        /// </summary>
        /// <param name="stream">The transport stream.</param>
        /// <param name="authenticationHandler">The authentication handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task PerformUnixLoginAuthenticationAsync(Stream stream, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            // Unix login authentication uses system credentials
            CredentialsAuthenticationInput input = await authenticationHandler
                .ProvideAuthenticationInputAsync(_context.Connection, this, new CredentialsAuthenticationInputRequest()).ConfigureAwait(false);

            // Format: username_length + username + password_length + password
            byte[] usernameBytes = Encoding.UTF8.GetBytes(input.Username ?? string.Empty);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(input.Password ?? string.Empty);

            // Send username
            await stream.WriteAsync(new[] { (byte)usernameBytes.Length }, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(usernameBytes, cancellationToken).ConfigureAwait(false);

            // Send password
            await stream.WriteAsync(new[] { (byte)passwordBytes.Length }, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(passwordBytes, cancellationToken).ConfigureAwait(false);

            // Clear sensitive data
            Array.Clear(passwordBytes, 0, passwordBytes.Length);
        }

        /// <summary>
        /// Creates a VNC-style DES encrypted response.
        /// </summary>
        /// <param name="challenge">The challenge from the server.</param>
        /// <param name="password">The password.</param>
        /// <returns>The encrypted response.</returns>
        private static byte[] CreateVncStyleResponse(byte[] challenge, string password)
        {
            // Use the first 8 characters/bytes of the password as the DES key
            var key = new byte[8];
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
            Array.Copy(passwordBytes, key, Math.Min(key.Length, passwordBytes.Length));

            // Reverse bit order of all bytes in key (VNC requirement)
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

            // Encrypt challenge with DES
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

            return response;
        }
    }
}
