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
    /// A security type that implements Simple Authentication and Security Layer (SASL) authentication.
    /// This is primarily used by GTK-VNC and provides flexible authentication mechanisms.
    /// </summary>
    public class SaslSecurityType : ISecurityType
    {
        private readonly RfbConnectionContext _context;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownSecurityType.SASL;

        /// <inheritdoc />
        public string Name => "SASL";

        /// <inheritdoc />
        public int Priority => 60; // Good security with flexible mechanisms

        /// <summary>
        /// Initializes a new instance of the <see cref="SaslSecurityType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public SaslSecurityType(RfbConnectionContext context)
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

            // Step 1: Read list of supported SASL mechanisms from server
            List<string> mechanisms = await ReadSaslMechanismsAsync(transport.Stream, cancellationToken).ConfigureAwait(false);

            if (mechanisms.Count == 0)
                throw new InvalidOperationException("Server provided no SASL mechanisms.");

            // Step 2: Choose the best available mechanism (prefer PLAIN for simplicity)
            string chosenMechanism = ChoosePreferredMechanism(mechanisms);
            if (string.IsNullOrEmpty(chosenMechanism))
                throw new InvalidOperationException("No supported SASL mechanism found.");

            // Step 3: Send chosen mechanism to server
            await SendChosenMechanismAsync(transport.Stream, chosenMechanism, cancellationToken).ConfigureAwait(false);

            // Step 4: Perform authentication based on chosen mechanism
            await PerformSaslAuthenticationAsync(transport.Stream, chosenMechanism, authenticationHandler, cancellationToken).ConfigureAwait(false);

            return new AuthenticationResult();
        }

        /// <inheritdoc />
        public Task ReadServerInitExtensionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <summary>
        /// Reads the list of SASL mechanisms supported by the server.
        /// </summary>
        /// <param name="stream">The transport stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>List of supported SASL mechanism names.</returns>
        private static async Task<List<string>> ReadSaslMechanismsAsync(Stream stream, CancellationToken cancellationToken)
        {
            var mechanisms = new List<string>();

            // Read the number of mechanisms (1 byte)
            var countBuffer = new byte[1];
            await stream.ReadExactlyAsync(countBuffer, cancellationToken).ConfigureAwait(false);
            int mechanismCount = countBuffer[0];

            // Read each mechanism
            for (int i = 0; i < mechanismCount; i++)
            {
                // Read mechanism name length (1 byte)
                var lengthBuffer = new byte[1];
                await stream.ReadExactlyAsync(lengthBuffer, cancellationToken).ConfigureAwait(false);
                int nameLength = lengthBuffer[0];

                if (nameLength > 0)
                {
                    // Read mechanism name
                    var nameBuffer = new byte[nameLength];
                    await stream.ReadExactlyAsync(nameBuffer, cancellationToken).ConfigureAwait(false);
                    string mechanismName = Encoding.UTF8.GetString(nameBuffer);
                    mechanisms.Add(mechanismName);
                }
            }

            return mechanisms;
        }

        /// <summary>
        /// Chooses the preferred SASL mechanism from the available options.
        /// </summary>
        /// <param name="mechanisms">Available SASL mechanisms.</param>
        /// <returns>The chosen mechanism name.</returns>
        private static string ChoosePreferredMechanism(List<string> mechanisms)
        {
            // Prefer mechanisms in this order (most secure/supported first)
            string[] preferredOrder = { "SCRAM-SHA-256", "SCRAM-SHA-1", "DIGEST-MD5", "CRAM-MD5", "PLAIN" };

            foreach (string preferred in preferredOrder)
            {
                if (mechanisms.Contains(preferred, StringComparer.OrdinalIgnoreCase))
                    return preferred;
            }

            // If none of the preferred mechanisms are available, return the first one
            return mechanisms.FirstOrDefault() ?? string.Empty;
        }

        /// <summary>
        /// Sends the chosen SASL mechanism to the server.
        /// </summary>
        /// <param name="stream">The transport stream.</param>
        /// <param name="mechanism">The chosen mechanism name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private static async Task SendChosenMechanismAsync(Stream stream, string mechanism, CancellationToken cancellationToken)
        {
            byte[] mechanismBytes = Encoding.UTF8.GetBytes(mechanism);

            // Send mechanism name length (1 byte)
            await stream.WriteAsync(new[] { (byte)mechanismBytes.Length }, cancellationToken).ConfigureAwait(false);

            // Send mechanism name
            await stream.WriteAsync(mechanismBytes, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs SASL authentication based on the chosen mechanism.
        /// </summary>
        /// <param name="stream">The transport stream.</param>
        /// <param name="mechanism">The chosen SASL mechanism.</param>
        /// <param name="authenticationHandler">The authentication handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task PerformSaslAuthenticationAsync(Stream stream, string mechanism, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            switch (mechanism.ToUpperInvariant())
            {
                case "PLAIN":
                    await PerformPlainAuthenticationAsync(stream, authenticationHandler, cancellationToken).ConfigureAwait(false);
                    break;

                case "CRAM-MD5":
                    await PerformCramMd5AuthenticationAsync(stream, authenticationHandler, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    // For unsupported mechanisms, attempt basic PLAIN-style authentication
                    // This may not work with all mechanisms but provides a fallback
                    await PerformPlainAuthenticationAsync(stream, authenticationHandler, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        /// <summary>
        /// Performs PLAIN SASL authentication.
        /// </summary>
        /// <param name="stream">The transport stream.</param>
        /// <param name="authenticationHandler">The authentication handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task PerformPlainAuthenticationAsync(Stream stream, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            // Get credentials from authentication handler
            CredentialsAuthenticationInput input = await authenticationHandler
                .ProvideAuthenticationInputAsync(_context.Connection, this, new CredentialsAuthenticationInputRequest()).ConfigureAwait(false);

            // PLAIN SASL format: [authzid] NUL authcid NUL passwd
            // For VNC, we typically use: NUL username NUL password
            string plainAuth = $"\0{input.Username}\0{input.Password}";
            byte[] authBytes = Encoding.UTF8.GetBytes(plainAuth);

            // Send authentication data length (4 bytes, big-endian)
            byte[] lengthBytes = BitConverter.GetBytes((uint)authBytes.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            await stream.WriteAsync(lengthBytes, cancellationToken).ConfigureAwait(false);

            // Send authentication data
            await stream.WriteAsync(authBytes, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            // Clear sensitive data
            Array.Clear(authBytes, 0, authBytes.Length);

            // Read authentication result (1 byte: 0 = success, 1 = failure)
            var resultBuffer = new byte[1];
            await stream.ReadExactlyAsync(resultBuffer, cancellationToken).ConfigureAwait(false);

            if (resultBuffer[0] != 0)
                throw new InvalidOperationException("SASL PLAIN authentication failed.");
        }

        /// <summary>
        /// Performs CRAM-MD5 SASL authentication.
        /// </summary>
        /// <param name="stream">The transport stream.</param>
        /// <param name="authenticationHandler">The authentication handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task PerformCramMd5AuthenticationAsync(Stream stream, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            // Read challenge length (4 bytes, big-endian)
            var lengthBuffer = new byte[4];
            await stream.ReadExactlyAsync(lengthBuffer, cancellationToken).ConfigureAwait(false);
            
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBuffer);
            uint challengeLength = BitConverter.ToUInt32(lengthBuffer, 0);

            // Read challenge
            var challengeBuffer = new byte[challengeLength];
            await stream.ReadExactlyAsync(challengeBuffer, cancellationToken).ConfigureAwait(false);
            string challenge = Encoding.UTF8.GetString(challengeBuffer);

            // Get credentials
            CredentialsAuthenticationInput input = await authenticationHandler
                .ProvideAuthenticationInputAsync(_context.Connection, this, new CredentialsAuthenticationInputRequest()).ConfigureAwait(false);

            // Compute CRAM-MD5 response: username + " " + HMAC-MD5(password, challenge)
            using var hmac = new System.Security.Cryptography.HMACMD5(Encoding.UTF8.GetBytes(input.Password));
            byte[] hashBytes = hmac.ComputeHash(challengeBuffer);
            string hexHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            string response = $"{input.Username} {hexHash}";

            byte[] responseBytes = Encoding.UTF8.GetBytes(response);

            // Send response length (4 bytes, big-endian)
            byte[] responseLengthBytes = BitConverter.GetBytes((uint)responseBytes.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(responseLengthBytes);

            await stream.WriteAsync(responseLengthBytes, cancellationToken).ConfigureAwait(false);

            // Send response
            await stream.WriteAsync(responseBytes, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            // Clear sensitive data
            Array.Clear(responseBytes, 0, responseBytes.Length);

            // Read authentication result (1 byte: 0 = success, 1 = failure)
            var resultBuffer = new byte[1];
            await stream.ReadExactlyAsync(resultBuffer, cancellationToken).ConfigureAwait(false);

            if (resultBuffer[0] != 0)
                throw new InvalidOperationException("SASL CRAM-MD5 authentication failed.");
        }
    }
}
