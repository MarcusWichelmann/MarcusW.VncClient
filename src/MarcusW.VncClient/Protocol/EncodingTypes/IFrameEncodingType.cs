using System.IO;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Protocol.EncodingTypes
{
    /// <summary>
    /// Represents a RFB protocol encoding type for (part of) a frame.
    /// </summary>
    public interface IFrameEncodingType : IEncodingType
    {
        /// <summary>
        /// Reads a (partial) frame from the transport stream, decodes it and renders it to the target framebuffer, if available.
        /// </summary>
        /// <param name="transportStream">The stream to read from.</param>
        /// <param name="targetFramebuffer">The target framebuffer reference, or null if unavailable.</param>
        /// <param name="rectangle">The part of the frame to update.</param>
        /// <param name="remoteFramebufferSize">The current size of the remote framebuffer.</param>
        /// <param name="remoteFramebufferFormat">The current pixel format.</param>
        void ReadFrameEncoding(Stream transportStream, IFramebufferReference? targetFramebuffer, in Rectangle rectangle, in Size remoteFramebufferSize,
            in PixelFormat remoteFramebufferFormat);
    }
}
