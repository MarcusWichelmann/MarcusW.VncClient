namespace MarcusW.VncClient.Protocol.MessageTypes
{
    /// <summary>
    /// Represents a RFB protocol message type.
    /// </summary>
    public interface IMessageType
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
        /// Gets whether this is a standard message type that must be supported by all servers and clients.
        /// </summary>
        bool IsStandardMessageType { get; }
    }
}
