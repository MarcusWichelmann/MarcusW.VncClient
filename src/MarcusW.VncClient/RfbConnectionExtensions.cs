using System;
using System.Threading;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Extension methods for <see cref="RfbConnection"/>.
    /// </summary>
    public static class RfbConnectionExtensions
    {
        /// <summary>
        /// Sends clipboard text to the VNC server.
        /// </summary>
        /// <param name="connection">The RFB connection.</param>
        /// <param name="text">The clipboard text to send.</param>
        /// <param name="cancellationToken">A cancellation token to stop the operation.</param>
        /// <returns>True if the message was enqueued successfully, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when connection or text is null.</exception>
        public static bool SendClipboardText(this RfbConnection connection, string text, CancellationToken cancellationToken = default)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var message = new ClientCutTextMessage(text);
            return connection.EnqueueMessage(message, cancellationToken);
        }

        /// <summary>
        /// Sends an xvp shutdown command to the VNC server.
        /// The server must support the xvp extension for this to work.
        /// </summary>
        /// <param name="connection">The RFB connection.</param>
        /// <param name="cancellationToken">A cancellation token to stop the operation.</param>
        /// <returns>True if the message was enqueued successfully, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when connection is null.</exception>
        public static bool SendXvpShutdown(this RfbConnection connection, CancellationToken cancellationToken = default)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var message = new XvpClientMessage(XvpOperation.Shutdown);
            return connection.EnqueueMessage(message, cancellationToken);
        }

        /// <summary>
        /// Sends an xvp reboot command to the VNC server.
        /// The server must support the xvp extension for this to work.
        /// </summary>
        /// <param name="connection">The RFB connection.</param>
        /// <param name="cancellationToken">A cancellation token to stop the operation.</param>
        /// <returns>True if the message was enqueued successfully, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when connection is null.</exception>
        public static bool SendXvpReboot(this RfbConnection connection, CancellationToken cancellationToken = default)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var message = new XvpClientMessage(XvpOperation.Reboot);
            return connection.EnqueueMessage(message, cancellationToken);
        }

        /// <summary>
        /// Sends an xvp reset command to the VNC server.
        /// The server must support the xvp extension for this to work.
        /// </summary>
        /// <param name="connection">The RFB connection.</param>
        /// <param name="cancellationToken">A cancellation token to stop the operation.</param>
        /// <returns>True if the message was enqueued successfully, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when connection is null.</exception>
        public static bool SendXvpReset(this RfbConnection connection, CancellationToken cancellationToken = default)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var message = new XvpClientMessage(XvpOperation.Reset);
            return connection.EnqueueMessage(message, cancellationToken);
        }
    }
}
