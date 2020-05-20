namespace MarcusW.VncClient.Protocol.Services.Communication
{
    /// <summary>
    /// Provides methods for sending RFB protocol messages.
    /// </summary>
    internal class RfbMessageSender : IRfbMessageSender
    {
        // TODO: Use queue for sending messages? Advantages? Because of easier fire&forget? -> BackgroundThread & IBackgroundThread
        // TODO: Provide methods for either just pushing messages to the send queue or waiting for the message having been sent (TaskCompletionSource)

        public RfbMessageSender() { }
    }
}
