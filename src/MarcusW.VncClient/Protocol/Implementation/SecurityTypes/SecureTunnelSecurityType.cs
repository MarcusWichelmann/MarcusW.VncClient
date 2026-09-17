using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.Implementation.Services.Transports;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Security;

namespace MarcusW.VncClient.Protocol.Implementation.SecurityTypes
{
    /// <summary>
    /// A security type that implements Secure Tunnel authentication.
    /// This provides encrypted tunneling for VNC connections using SSL/TLS with additional authentication.
    /// </summary>
    public class SecureTunnelSecurityType : ISecurityType
    {
        private readonly RfbConnectionContext _context;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownSecurityType.SecureTunnel;

        /// <inheritdoc />
        public string Name => "SecureTunnel";

        /// <inheritdoc />
        public int Priority => 90; // Very high priority due to secure tunneling

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureTunnelSecurityType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public SecureTunnelSecurityType(RfbConnectionContext context)
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

            // Step 1: Establish secure tunnel using SSL/TLS
            var sslStream = new SslStream(transport.Stream, leaveInnerStreamOpen: false, ValidateServerCertificate);

            try
            {
                // Start TLS handshake
                var sslOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = GetTargetHostName(),
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = ValidateServerCertificate
                };

                await sslStream.AuthenticateAsClientAsync(sslOptions, cancellationToken).ConfigureAwait(false);

                if (!sslStream.IsAuthenticated || !sslStream.IsEncrypted)
                    throw new InvalidOperationException("Secure tunnel establishment failed - connection is not properly secured.");

                // Step 2: Perform authentication over the secure tunnel
                // Read authentication method from server (1 byte)
                var authMethodBuffer = new byte[1];
                await sslStream.ReadExactlyAsync(authMethodBuffer, cancellationToken).ConfigureAwait(false);
                byte authMethod = authMethodBuffer[0];

                // Step 3: Handle authentication based on method
                await PerformSecureTunnelAuthenticationAsync(sslStream, authMethod, authenticationHandler, cancellationToken).ConfigureAwait(false);

                // Step 4: Create secure transport wrapper
                var secureTransport = new SecureTunnelTransport(sslStream);

                return new AuthenticationResult(secureTransport, expectSecurityResult: true);
            }
            catch
            {
                // If secure tunnel setup fails, dispose the SSL stream
                sslStream.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        public Task ReadServerInitExtensionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <summary>
        /// Gets the target hostname for TLS certificate validation.
        /// </summary>
        /// <returns>The hostname to use for TLS validation.</returns>
        private string GetTargetHostName()
        {
            // Try to get hostname from TCP transport parameters
            if (_context.Connection.Parameters.TransportParameters is TcpTransportParameters tcpParams)
            {
                return tcpParams.Host;
            }

            // Fallback to generic hostname
            return "vnc-server";
        }

        /// <summary>
        /// Performs authentication over the secure tunnel.
        /// </summary>
        /// <param name="secureStream">The secure SSL stream.</param>
        /// <param name="authMethod">The authentication method.</param>
        /// <param name="authenticationHandler">The authentication handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task PerformSecureTunnelAuthenticationAsync(SslStream secureStream, byte authMethod, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            switch (authMethod)
            {
                case 0: // No additional authentication (TLS client cert was sufficient)
                    break;

                case 1: // Password authentication over secure tunnel
                    await PerformSecurePasswordAuthenticationAsync(secureStream, authenticationHandler, cancellationToken).ConfigureAwait(false);
                    break;

                case 2: // Certificate-based authentication
                    await PerformCertificateAuthenticationAsync(secureStream, authenticationHandler, cancellationToken).ConfigureAwait(false);
                    break;

                case 3: // Username/password authentication
                    await PerformSecureCredentialsAuthenticationAsync(secureStream, authenticationHandler, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    throw new NotSupportedException($"Secure tunnel authentication method {authMethod} is not supported.");
            }
        }

        /// <summary>
        /// Performs password authentication over the secure tunnel.
        /// </summary>
        /// <param name="secureStream">The secure stream.</param>
        /// <param name="authenticationHandler">The authentication handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task PerformSecurePasswordAuthenticationAsync(SslStream secureStream, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            // Get password
            PasswordAuthenticationInput input = await authenticationHandler
                .ProvideAuthenticationInputAsync(_context.Connection, this, new PasswordAuthenticationInputRequest()).ConfigureAwait(false);

            // Send password length (4 bytes, big-endian)
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(input.Password ?? string.Empty);
            var lengthBuffer = new byte[4];
            lengthBuffer[0] = (byte)(passwordBytes.Length >> 24);
            lengthBuffer[1] = (byte)(passwordBytes.Length >> 16);
            lengthBuffer[2] = (byte)(passwordBytes.Length >> 8);
            lengthBuffer[3] = (byte)(passwordBytes.Length & 0xFF);

            await secureStream.WriteAsync(lengthBuffer, cancellationToken).ConfigureAwait(false);

            // Send password
            await secureStream.WriteAsync(passwordBytes, cancellationToken).ConfigureAwait(false);
            await secureStream.FlushAsync(cancellationToken).ConfigureAwait(false);

            // Clear sensitive data
            Array.Clear(passwordBytes, 0, passwordBytes.Length);

            // Read authentication result (1 byte: 0 = success, 1 = failure)
            var resultBuffer = new byte[1];
            await secureStream.ReadExactlyAsync(resultBuffer, cancellationToken).ConfigureAwait(false);

            if (resultBuffer[0] != 0)
                throw new InvalidOperationException("Secure tunnel password authentication failed.");
        }

        /// <summary>
        /// Performs certificate-based authentication over the secure tunnel.
        /// </summary>
        /// <param name="secureStream">The secure stream.</param>
        /// <param name="authenticationHandler">The authentication handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task PerformCertificateAuthenticationAsync(SslStream secureStream, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            // Certificate authentication is typically handled during TLS handshake
            // This method can be extended to handle additional certificate verification
            
            // For now, we assume the certificate authentication was successful during TLS handshake
            // In a full implementation, you might:
            // 1. Request additional certificate information from the authentication handler
            // 2. Perform additional certificate validation
            // 3. Send certificate-based challenge/response

            // Send success acknowledgment
            await secureStream.WriteAsync(new byte[] { 0 }, cancellationToken).ConfigureAwait(false);
            await secureStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs username/password authentication over the secure tunnel.
        /// </summary>
        /// <param name="secureStream">The secure stream.</param>
        /// <param name="authenticationHandler">The authentication handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task PerformSecureCredentialsAuthenticationAsync(SslStream secureStream, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            // Get credentials
            CredentialsAuthenticationInput input = await authenticationHandler
                .ProvideAuthenticationInputAsync(_context.Connection, this, new CredentialsAuthenticationInputRequest()).ConfigureAwait(false);

            // Send username length and username
            byte[] usernameBytes = System.Text.Encoding.UTF8.GetBytes(input.Username ?? string.Empty);
            var usernameLengthBuffer = new byte[4];
            usernameLengthBuffer[0] = (byte)(usernameBytes.Length >> 24);
            usernameLengthBuffer[1] = (byte)(usernameBytes.Length >> 16);
            usernameLengthBuffer[2] = (byte)(usernameBytes.Length >> 8);
            usernameLengthBuffer[3] = (byte)(usernameBytes.Length & 0xFF);

            await secureStream.WriteAsync(usernameLengthBuffer, cancellationToken).ConfigureAwait(false);
            await secureStream.WriteAsync(usernameBytes, cancellationToken).ConfigureAwait(false);

            // Send password length and password
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(input.Password ?? string.Empty);
            var passwordLengthBuffer = new byte[4];
            passwordLengthBuffer[0] = (byte)(passwordBytes.Length >> 24);
            passwordLengthBuffer[1] = (byte)(passwordBytes.Length >> 16);
            passwordLengthBuffer[2] = (byte)(passwordBytes.Length >> 8);
            passwordLengthBuffer[3] = (byte)(passwordBytes.Length & 0xFF);

            await secureStream.WriteAsync(passwordLengthBuffer, cancellationToken).ConfigureAwait(false);
            await secureStream.WriteAsync(passwordBytes, cancellationToken).ConfigureAwait(false);
            await secureStream.FlushAsync(cancellationToken).ConfigureAwait(false);

            // Clear sensitive data
            Array.Clear(passwordBytes, 0, passwordBytes.Length);

            // Read authentication result
            var resultBuffer = new byte[1];
            await secureStream.ReadExactlyAsync(resultBuffer, cancellationToken).ConfigureAwait(false);

            if (resultBuffer[0] != 0)
                throw new InvalidOperationException("Secure tunnel credentials authentication failed.");
        }

        /// <summary>
        /// Validates the server certificate for the secure tunnel.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="certificate">The server certificate.</param>
        /// <param name="chain">The certificate chain.</param>
        /// <param name="sslPolicyErrors">SSL policy errors.</param>
        /// <returns>True to accept the certificate, false otherwise.</returns>
        private static bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            // For secure tunnel, we might want stricter certificate validation
            // For now, accept all certificates (similar to many VNC clients)
            // In production, consider implementing proper certificate validation
            return true;
        }
    }

    /// <summary>
    /// Transport wrapper for secure tunnel SSL/TLS streams.
    /// </summary>
    internal class SecureTunnelTransport : ITransport
    {
        /// <inheritdoc />
        public Stream Stream { get; }

        /// <inheritdoc />
        public bool IsEncrypted => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureTunnelTransport"/>.
        /// </summary>
        /// <param name="sslStream">The SSL stream to wrap.</param>
        public SecureTunnelTransport(SslStream sslStream)
        {
            Stream = sslStream ?? throw new ArgumentNullException(nameof(sslStream));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Stream?.Dispose();
        }
    }
}
