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
    /// A security type that establishes a TLS/SSL encrypted tunnel for VNC communication.
    /// This provides transport-level encryption but no client authentication.
    /// </summary>
    public class TlsSecurityType : ISecurityType
    {
        private readonly RfbConnectionContext _context;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownSecurityType.TLS;

        /// <inheritdoc />
        public string Name => "TLS";

        /// <inheritdoc />
        public int Priority => 80; // High priority due to encryption, but lower than VeNCrypt which offers more options

        /// <summary>
        /// Initializes a new instance of the <see cref="TlsSecurityType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public TlsSecurityType(RfbConnectionContext context)
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

            // Create SSL stream over the existing transport
            var sslStream = new SslStream(transport.Stream, leaveInnerStreamOpen: false, ValidateServerCertificate);

            try
            {
                // Start TLS handshake as client
                var sslOptions = new SslClientAuthenticationOptions
                {
                    // Use the connection hostname if available, otherwise use a generic name
                    TargetHost = GetTargetHostName(),
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck, // Often VNC servers use self-signed certificates
                    RemoteCertificateValidationCallback = ValidateServerCertificate
                };

                await sslStream.AuthenticateAsClientAsync(sslOptions, cancellationToken).ConfigureAwait(false);

                if (!sslStream.IsAuthenticated || !sslStream.IsEncrypted)
                    throw new InvalidOperationException("TLS authentication failed - connection is not properly secured.");

                // Create new transport wrapper for the SSL stream
                var secureTransport = new SslTransport(sslStream);

                // TLS security type only provides transport encryption, no authentication is expected afterward
                return new AuthenticationResult(secureTransport, expectSecurityResult: true);
            }
            catch
            {
                // If TLS setup fails, dispose the SSL stream
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
        /// Validates the server certificate. This is a basic implementation that accepts all certificates.
        /// In production environments, you may want to implement proper certificate validation.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="certificate">The server certificate.</param>
        /// <param name="chain">The certificate chain.</param>
        /// <param name="sslPolicyErrors">SSL policy errors.</param>
        /// <returns>Always returns true to accept all certificates.</returns>
        private static bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            // For VNC connections, servers often use self-signed certificates
            // In a production environment, you might want to:
            // 1. Check against a known certificate fingerprint
            // 2. Prompt the user to accept unknown certificates
            // 3. Implement certificate pinning
            
            // For now, accept all certificates (similar to many VNC clients)
            return true;
        }
    }

    /// <summary>
    /// Transport wrapper for SSL/TLS streams.
    /// </summary>
    internal class SslTransport : ITransport
    {
        /// <inheritdoc />
        public Stream Stream { get; }

        /// <inheritdoc />
        public bool IsEncrypted => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="SslTransport"/>.
        /// </summary>
        /// <param name="sslStream">The SSL stream to wrap.</param>
        public SslTransport(SslStream sslStream)
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
