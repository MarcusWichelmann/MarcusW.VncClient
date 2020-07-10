namespace MarcusW.VncClient.Protocol.MessageTypes
{
    /// <summary>
    /// Represents a server-to-client message type of the RFB protocol.
    /// </summary>
    public interface IIncomingMessageType : IMessageType
    {
        /// <summary>
        /// Reads the message from the transport stream and processes it.
        /// </summary>
        /// <param name="transport">The transport to read from.</param>
        void ReadMessage(ITransport transport);
    }
}
