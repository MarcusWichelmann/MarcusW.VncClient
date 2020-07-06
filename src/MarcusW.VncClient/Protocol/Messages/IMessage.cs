namespace MarcusW.VncClient.Protocol.Messages
{
    /// <summary>
    /// Represents a RFB protocol message.
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// Gets the ID for this message type.
        /// </summary>
        byte Id { get; }

        /// <summary>
        /// Gets a human readable name for this message type.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets whether this is a standard message that must be supported by all servers and clients.
        /// </summary>
        bool IsStandardMessage { get; }
    }
}
