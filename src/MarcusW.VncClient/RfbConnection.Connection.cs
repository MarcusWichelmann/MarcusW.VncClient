using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Utils;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient
{
    public partial class RfbConnection
    {
        private RfbConnectionContext? _activeConnection;

        // Note: The connection management methods below should only be called in a synchronized manner!
        // In this case, this is ensured by the main part of this class.

        // Used for establishing the initial connection as well as for reconnects.
        private async Task EstablishNewConnectionAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Doing one more cleanup won't hurt.
            CleanupPreviousConnection();

            _logger.LogInformation("Connecting to VNC-Server on {endpoint}...", Parameters.Endpoint);

            // Create a new connection context
            var context = new RfbConnectionContext(this);

            try
            {
                // Establish a new transport connection
                context.Transport = await ProtocolImplementation.CreateTransportConnector(context).ConnectAsync(cancellationToken).ConfigureAwait(false);

                // Do the handshake
                context.HandshakeResult = await ProtocolImplementation.CreateRfbHandshaker(context).DoHandshakeAsync(cancellationToken).ConfigureAwait(false);

                // TODO: Initialization, ...

                // Setup new receive loop
                context.MessageReceiver = ProtocolImplementation.CreateMessageReceiver(context);
                context.MessageReceiver.StartReceiveLoop();
            }
            catch
            {
                // Ensure cleanup on failure
                context.MessageReceiver?.Dispose();
                context.Transport?.Dispose();

                throw;
            }

            // From now on, exceptions will only land in the Failed event handler.
            // This should be the last real operation to ensure that exceptions are not propagated by two ways at the same time.
            context.MessageReceiver.Failed += MessageReceiverOnFailed;

            _activeConnection = context;

            _logger.LogInformation("Connection to {endpoint} established successfully.", Parameters.Endpoint);
        }

        private async Task CloseConnectionAsync()
        {
            if (_activeConnection == null)
                return;

            _logger.LogInformation("Closing connection to {endpoint}...", Parameters.Endpoint);

            // Stop receiving first
            if (_activeConnection.MessageReceiver != null)
                await _activeConnection.MessageReceiver.StopReceiveLoopAsync().ConfigureAwait(false);

            // Close connection
            _activeConnection.Transport?.Dispose();

            CleanupPreviousConnection();
        }

        private void CleanupPreviousConnection()
        {
            if (_activeConnection == null)
                return;

            if (_activeConnection.MessageReceiver != null)
            {
                _activeConnection.MessageReceiver.Failed -= MessageReceiverOnFailed;
                _activeConnection.MessageReceiver.Dispose();
                _activeConnection.MessageReceiver = null;
            }

            if (_activeConnection.Transport != null)
            {
                _activeConnection.Transport.Dispose();
                _activeConnection.Transport = null;
            }

            _activeConnection = null;
        }

        // Forward to main class part
        private void MessageReceiverOnFailed(object? sender, BackgroundThreadFailedEventArgs e) => OnRunningConnectionFailed();
    }
}
