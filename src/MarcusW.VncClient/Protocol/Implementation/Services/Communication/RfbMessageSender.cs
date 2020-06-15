using MarcusW.VncClient.Protocol.Services;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Communication
{
    /// <inheritdoc />
    public class RfbMessageSender : IRfbMessageSender
    {
        // TODO: Use queue for sending messages? Advantages? Because of easier fire&forget? -> BackgroundThread & IBackgroundThread
        // TODO: Provide methods for either just pushing messages to the send queue or waiting for the message having been sent (TaskCompletionSource)

        /// <summary>
        /// Initializes a new instance of the <see cref="RfbMessageSender"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public RfbMessageSender(RfbConnectionContext context) { }
    }
}
