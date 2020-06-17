namespace MarcusW.VncClient.Protocol.Encodings
{
    /// <summary>
    /// Represents a RFB protocol encoding.
    /// </summary>
    public interface IEncoding
    {
        /// <summary>
        /// Gets the ID for this encoding.
        /// </summary>
        int Id { get; }
    }
}
