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
        /// Gets whether the server will confirm somehow that it supports this encoding type,
        /// or if we just expect it to be supported by the server without knowing, if the server actually understood
        /// the encoding type we requested during SetEncodings.
        /// </summary>
        bool GetsConfirmed { get; }
    }
}
