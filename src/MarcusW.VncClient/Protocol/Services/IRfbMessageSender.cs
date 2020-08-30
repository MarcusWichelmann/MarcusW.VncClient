using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.MessageTypes;
using MarcusW.VncClient.Utils;

namespace MarcusW.VncClient.Protocol.Services
{
    /// <summary>
    /// Describes a background thread that sends queued messages and provides methods to add messages to the send queue.
    /// </summary>
    public interface IRfbMessageSender : IBackgroundThread
    {
        /// <summary>
        /// Starts the send loop.
        /// </summary>
        void StartSendLoop();

        /// <summary>
        /// Stops the send loop and waits for completion.
        /// </summary>
        Task StopSendLoopAsync();

        /// <summary>
        /// Enqueues some initial messages to get things rolling.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        void EnqueueInitialMessages(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds the <paramref name="message"/> to the send queue and returns without waiting for it being sent.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="TMessageType">The type of the message.</typeparam>
        /// <remarks>Please ensure the outgoing message type is marked as being supported by both sides before sending it. See <see cref="RfbConnection.UsedMessageTypes"/>.</remarks>
        void EnqueueMessage<TMessageType>(IOutgoingMessage<TMessageType> message, CancellationToken cancellationToken = default) where TMessageType : class, IOutgoingMessageType;

        /// <summary>
        /// Adds the <paramref name="message"/> to the send queue and returns a <see cref="Task"/> that completes when the message was sent.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="TMessageType">The type of the message.</typeparam>
        /// <remarks>Please ensure the outgoing message type is marked as being supported by both sides before sending it. See <see cref="RfbConnection.UsedMessageTypes"/>.</remarks>
        Task SendMessageAsync<TMessageType>(IOutgoingMessage<TMessageType> message, CancellationToken cancellationToken = default) where TMessageType : class, IOutgoingMessageType;
    }
}
