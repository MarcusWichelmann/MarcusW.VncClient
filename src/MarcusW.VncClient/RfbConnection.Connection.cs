using System;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Protocol.Services;
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

            _logger.LogInformation("Connecting to VNC-Server on {endpoint}...", Parameters.TransportParameters);

            // Create a new connection context
            var context = new RfbConnectionContext(this);
            context.State = ProtocolImplementation.CreateStateObject(context);
            context.ZLibInflater = ProtocolImplementation.CreateZLibInflater(context);

            // Create message and encoding types collections in an order which allows e.g. the message types to get an overview about all registered encodings.
            context.SupportedEncodingTypes = ProtocolImplementation.CreateEncodingTypesCollection(context);
            context.SupportedMessageTypes = ProtocolImplementation.CreateMessageTypesCollection(context);
            context.SupportedSecurityTypes = ProtocolImplementation.CreateSecurityTypesCollection(context);

            // Prepare the state for first use
            context.State.Prepare();

            try
            {
                // Establish a new transport connection
                context.Transport = await ProtocolImplementation.CreateTransportConnector(context).ConnectAsync(cancellationToken).ConfigureAwait(false);

                // Do the handshake
                ITransport? tunnelTransport = await ProtocolImplementation.CreateRfbHandshaker(context).DoHandshakeAsync(cancellationToken).ConfigureAwait(false);

                // Replace the current transport in case a tunnel has been built during handshake
                if (tunnelTransport != null)
                    context.Transport = tunnelTransport;

                // Initialize the connection
                await ProtocolImplementation.CreateRfbInitializer(context).InitializeAsync(cancellationToken).ConfigureAwait(false);

                // Setup send and receive loops
                context.MessageReceiver = ProtocolImplementation.CreateMessageReceiver(context);
                context.MessageSender = ProtocolImplementation.CreateMessageSender(context);
                context.MessageSender.EnqueueInitialMessages(cancellationToken);
                context.MessageReceiver.StartReceiveLoop();
                context.MessageSender.StartSendLoop();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Connecting to VNC-Server on {endpoint} failed: {exception}", Parameters.TransportParameters, ex.Message);

                // Ensure cleanup on failure
                context.MessageReceiver?.Dispose();
                context.MessageSender?.Dispose();
                context.Transport?.Dispose();

                throw;
            }

            // From now on, exceptions will only land in the Failed event handler.
            // This should be the last real operation to ensure that exceptions are not propagated by two ways at the same time.
            context.MessageReceiver.Failed += BackgroundThreadOnFailed;
            context.MessageSender.Failed += BackgroundThreadOnFailed;

            _activeConnection = context;

            _logger.LogInformation("Connection to {endpoint} established successfully.", Parameters.TransportParameters);
        }

        private async Task CloseConnectionAsync()
        {
            if (_activeConnection == null)
                return;

            _logger.LogInformation("Closing connection to {endpoint}...", Parameters.TransportParameters);

            // Stop receiving and sending
            if (_activeConnection.MessageReceiver != null)
                await _activeConnection.MessageReceiver.StopReceiveLoopAsync().ConfigureAwait(false);
            if (_activeConnection.MessageSender != null)
                await _activeConnection.MessageSender.StopSendLoopAsync().ConfigureAwait(false);

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
                _activeConnection.MessageReceiver.Failed -= BackgroundThreadOnFailed;
                _activeConnection.MessageReceiver.Dispose();
                _activeConnection.MessageReceiver = null;
            }

            if (_activeConnection.MessageSender != null)
            {
                _activeConnection.MessageSender.Failed -= BackgroundThreadOnFailed;
                _activeConnection.MessageSender.Dispose();
                _activeConnection.MessageSender = null;
            }

            _activeConnection.Transport?.Dispose();
            _activeConnection.Transport = null;

            _activeConnection.ZLibInflater?.Dispose();
            _activeConnection.ZLibInflater = null;

            _activeConnection = null;
        }

        private void BackgroundThreadOnFailed(object? sender, BackgroundThreadFailedEventArgs e)
        {
            // Forward to main class part and ensure the event is processed in the thread pool and not in the background thread (that might be killed.)
            ThreadPool.QueueUserWorkItem(state => OnRunningConnectionFailed(e.Exception));
        }
    }
}
