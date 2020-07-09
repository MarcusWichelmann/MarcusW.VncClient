namespace MarcusW.VncClient.Protocol.MessageTypes
{
    /// <summary>
    /// Represents a client-to-server message of the RFB protocol.
    /// </summary>
    public interface IOutgoingMessage<out TMessageType> where TMessageType : class, IOutgoingMessageType
    {
        /// <summary>
        /// Gets the message type to which this message belongs to.
        /// </summary>
        TMessageType Type { get; }
    }
}
