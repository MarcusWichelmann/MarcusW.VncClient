using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.Services;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Connection
{
    /// <inheritdoc />
    public class TcpConnector : ITcpConnector
    {
        private readonly ConnectParameters _connectParameters;
        private readonly ILogger<TcpConnector> _logger;

        internal TcpConnector(RfbConnectionContext context) : this(context.Connection.Parameters, context.Connection.LoggerFactory.CreateLogger<TcpConnector>()) { }

        // For uint testing only
        internal TcpConnector(ConnectParameters connectParameters, ILogger<TcpConnector> logger)
        {
            _connectParameters = connectParameters;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<TcpClient> ConnectAsync(CancellationToken cancellationToken = default)
        {
            IPEndPoint endpoint = _connectParameters.Endpoint!;

            _logger.LogDebug($"Starting connect attempt to endpoint {endpoint}...");

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

            return tcpClient;
        }
    }
}
