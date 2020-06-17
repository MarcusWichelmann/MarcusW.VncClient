using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.Services;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Transports
{
    /// <inheritdoc />
    public class TransportConnector : ITransportConnector
    {
        private readonly ConnectParameters _connectParameters;
        private readonly ILogger<TransportConnector> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransportConnector"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public TransportConnector(RfbConnectionContext context) : this((context ?? throw new ArgumentNullException(nameof(context))).Connection.Parameters,
            context.Connection.LoggerFactory.CreateLogger<TransportConnector>()) { }

        // For uint testing only
        internal TransportConnector(ConnectParameters connectParameters, ILogger<TransportConnector> logger)
        {
            _connectParameters = connectParameters;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<ITransport> ConnectAsync(CancellationToken cancellationToken = default)
        {
            // TODO: Support different transport types (SSH?) by adding some parameters (dynamic endpoint type interface?) to ConnectParameters and differentiating here.
            return await ConnectTcpTransportAsync(_connectParameters.Endpoint!, cancellationToken).ConfigureAwait(false);
        }

        private async Task<TcpTransport> ConnectTcpTransportAsync(IPEndPoint endpoint, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Connecting to TCP endpoint {endpoint}...", endpoint);

            // Create a cancellation token source that cancels on timeout or manual cancel
            using var timeoutCts = new CancellationTokenSource(_connectParameters.ConnectTimeout);
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            CancellationToken linkedToken = connectCts.Token;
            linkedToken.ThrowIfCancellationRequested();

            var tcpClient = new TcpClient();
            try
            {
                // Close (equals Dispose) the client on cancellation to cancel connect attempt
                await using (linkedToken.Register(() => tcpClient.Close()))
                {
                    linkedToken.ThrowIfCancellationRequested();

                    // Try to connect
                    await tcpClient.ConnectAsync(endpoint.Address, endpoint.Port).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested)
            {
                // Operation was canceled by the caller
                throw new OperationCanceledException("Connect was canceled.", ex, cancellationToken);
            }
            catch when (timeoutCts.IsCancellationRequested)
            {
                // Connect threw an exception because of being disposed after the timeout.
                throw new TimeoutException("Connect timeout reached.");
            }

            return new TcpTransport(tcpClient);
        }
    }
}
