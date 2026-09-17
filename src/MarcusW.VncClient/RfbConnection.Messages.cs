using System;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Protocol.MessageTypes;
using MarcusW.VncClient.Protocol.Services;

namespace MarcusW.VncClient
{
    public partial class RfbConnection
    {
        /// <summary>
        /// Tries to get the active message sender.
        /// </summary>
        /// <param name="messageSender">The message sender if available.</param>
        /// <returns>True if a message sender is available, otherwise false.</returns>
        private bool TryGetMessageSender(out IRfbMessageSender? messageSender)
        {
            messageSender = _activeConnection?.MessageSender;
            return messageSender != null;
        }

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

            if (!TryGetMessageSender(out var messageSender))
                return false;

            try
            {
                messageSender!.EnqueueMessage(message, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // The connection failed or was cleaned up concurrently (e.g. while a reconnect is in progress).
                // Just report that the message could not be queued instead of throwing into UI event handlers.
                return false;
            }

            return true;
        }

        /// <summary>
        /// Enqueues a framebuffer update request with throttling based on <see cref="ConnectParameters.FramebufferUpdateDelay"/>.
        /// </summary>
        /// <param name="rectangle">The rectangle to request an update for.</param>
        /// <param name="incremental">Whether this is an incremental update request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True, if the message was queued, otherwise false.</returns>
        public bool EnqueueFramebufferUpdateRequest(Rectangle rectangle, bool incremental, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryGetMessageSender(out var messageSender))
                return false;

            try
            {
                messageSender!.EnqueueFramebufferUpdateRequest(rectangle, incremental, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // Sender disappeared concurrently, see EnqueueMessage.
                return false;
            }

            return true;
        }

        /// <summary>
        /// Enqueues a framebuffer update request with a specific delay, ignoring global throttle settings.
        /// </summary>
        /// <param name="rectangle">The rectangle to request an update for.</param>
        /// <param name="incremental">Whether this is an incremental update request.</param>
        /// <param name="delay">The delay before sending the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True, if the message was queued, otherwise false.</returns>
        public bool EnqueueFramebufferUpdateRequestDelayed(Rectangle rectangle, bool incremental, TimeSpan delay, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryGetMessageSender(out var messageSender))
                return false;

            try
            {
                messageSender!.EnqueueFramebufferUpdateRequestDelayed(rectangle, incremental, delay, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // Sender disappeared concurrently, see EnqueueMessage.
                return false;
            }

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

            if (!TryGetMessageSender(out var messageSender))
                return Task.CompletedTask;

            try
            {
                return messageSender!.SendMessageAndWaitAsync(message, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // Sender disappeared concurrently, see EnqueueMessage. Behave like the "no sender" case above.
                return Task.CompletedTask;
            }
        }
    }
}
