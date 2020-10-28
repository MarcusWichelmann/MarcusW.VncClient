using System.IO;

namespace MarcusW.VncClient.Protocol.EncodingTypes
{
    /// <summary>
    /// Represents a RFB protocol encoding type for extended functionality.
    /// </summary>
    public interface IPseudoEncodingType : IEncodingType
    {
        /// <summary>
        /// Reads a pseudo encoding from the transport stream, decodes it and acts accordingly.
        /// </summary>
        /// <param name="transportStream">The stream to read from.</param>
        /// <param name="rectangle">The rectangle that was received with the pseudo encoding. Might be relevant sometimes.</param>
        void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle);
    }
}
