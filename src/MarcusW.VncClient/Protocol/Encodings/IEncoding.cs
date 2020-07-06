namespace MarcusW.VncClient.Protocol.Encodings
{
    /// <summary>
    /// Represents a RFB protocol encoding.
    /// </summary>
    public interface IEncoding
    {
        /// <summary>
        /// Gets the ID for this encoding type.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets a human readable name for this encoding type.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the priority value that represents the quality of this encoding.
        /// The server might prefer encodings with a higher priority.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets whether the encoding will only be used after the server sent some kind of confirmation that the encoding is supported.
        /// Encodings, that do not get confirmed, can still be sent, but the server might ignore them, if they are unsupported.
        /// </summary>
        bool RequiresConfirmation { get; }
    }
}
