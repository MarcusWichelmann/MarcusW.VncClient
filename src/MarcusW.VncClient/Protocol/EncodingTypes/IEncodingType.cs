namespace MarcusW.VncClient.Protocol.EncodingTypes
{
    /// <summary>
    /// Represents a RFB protocol encoding type.
    /// </summary>
    public interface IEncodingType
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
        /// Gets the priority value that represents the quality of this encoding type.
        /// The server might prefer encoding types with a higher priority.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets whether the encoding type will only be used after the server sent some kind of confirmation that the encoding type is supported.
        /// Encoding types, that do not get confirmed, can still be sent, but the server might ignore them, if they are unsupported.
        /// </summary>
        bool RequiresConfirmation { get; }
    }
}
