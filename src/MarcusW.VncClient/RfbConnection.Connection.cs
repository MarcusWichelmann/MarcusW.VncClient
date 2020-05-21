using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Protocol.Services.Communication;
using MarcusW.VncClient.Utils;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient
{
    public partial class RfbConnection
    {
        // Fundamental connection objects
        private TcpClient? _tcpClient;
        private IRfbMessageReceiver? _messageReceiver;

        // TODO: Message sender etc...

        // Used for establishing the initial connection as well as for reconnects.
        private async Task EstablishNewConnectionAsync(CancellationToken cancellationToken = default)
        {
            // Doing one more cleanup doesn't hurt.
            CleanupPreviousConnection();
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Connecting to VNC-Server on {endpoint}...", Parameters.Endpoint);

            // Establish TCP connection.
            _tcpClient = await ProtocolImplementation.CreateTcpConnector()
                .ConnectAsync(Parameters.Endpoint!, Parameters.ConnectTimeout, cancellationToken).ConfigureAwait(false);

            // TODO: Authenticate async

            // Setup new receive loop.
            _messageReceiver = ProtocolImplementation.CreateMessageReceiver(this);
            _messageReceiver.StartReceiveLoop();

            // From now on, exceptions will only land in the Failed event handler.
            // This should be the last real operation to ensure that exceptions are not propagated by two ways at the same time.
            _messageReceiver.Failed += MessageReceiverOnFailed;

            _logger.LogInformation("Connection to {endpoint} established successfully.", Parameters.Endpoint);
        }

        private async Task CloseConnectionAsync()
        {
            if (_tcpClient == null && _messageReceiver == null)
                return;

            _logger.LogInformation("Closing connection to {endpoint}...", Parameters.Endpoint);

            // Stop receiving first.
            if (_messageReceiver != null)
                await _messageReceiver.StopReceiveLoopAsync().ConfigureAwait(false);

            // Close connection
            _tcpClient?.Close();

            CleanupPreviousConnection();
        }

        private void CleanupPreviousConnection()
        {
            if (_messageReceiver != null)
            {
                _messageReceiver.Failed -= MessageReceiverOnFailed;
                _messageReceiver.Dispose();
                _messageReceiver = null;
            }

            if (_tcpClient != null)
            {
                _tcpClient.Dispose();
                _tcpClient = null;
            }
        }

        // Forward to main class part.
        private void MessageReceiverOnFailed(object? sender, BackgroundThreadFailedEventArgs e)
            => OnRunningConnectionFailed();
    }
}
