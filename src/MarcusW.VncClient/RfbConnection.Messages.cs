using System;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient
{
    public partial class RfbConnection
    {
        /// <summary>
        /// Adds the <paramref name="message"/> to the send queue and returns without waiting for it being sent.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="TMessageType">The type of the message.</typeparam>
        /// <returns>True, if the message was queued, otherwise false.</returns>
        /// <remarks>Please ensure the outgoing message type is marked as being supported by both sides before sending it. See <see cref="RfbConnection.UsedMessageTypes"/>.</remarks>
        public bool EnqueueMessage<TMessageType>(IOutgoingMessage<TMessageType> message, CancellationToken cancellationToken = default)
            where TMessageType : class, IOutgoingMessageType
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            RfbConnectionContext? connection = _activeConnection;
            if (connection?.MessageSender == null)
                return false;

            connection.MessageSender.EnqueueMessage(message, cancellationToken);
            return true;
        }

        /// <summary>
        /// Adds the <paramref name="message"/> to the send queue and returns a <see cref="Task"/> that completes when the message was sent.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="TMessageType">The type of the message.</typeparam>
        /// <remarks>Please ensure the outgoing message type is marked as being supported by both sides before sending it. See <see cref="RfbConnection.UsedMessageTypes"/>.</remarks>
        public Task SendMessageAsync<TMessageType>(IOutgoingMessage<TMessageType> message, CancellationToken cancellationToken = default)
            where TMessageType : class, IOutgoingMessageType
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            RfbConnectionContext? connection = _activeConnection;
            if (connection?.MessageSender == null)
                return Task.CompletedTask;

            return connection.MessageSender.SendMessageAndWaitAsync(message, cancellationToken);
        }
    }
}
