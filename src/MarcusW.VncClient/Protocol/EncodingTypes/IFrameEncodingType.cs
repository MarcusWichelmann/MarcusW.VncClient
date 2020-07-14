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
        /// Reads a (partial) frame from the transport stream, decodes it and renders it to the render target, if set.
        /// </summary>
        /// <param name="transportStream">The stream to read from.</param>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="rectangle">The part of the frame to update.</param>
        /// <param name="framebufferSize">The current size of the remote framebuffer.</param>
        /// <param name="framebufferFormat">The current pixel format.</param>
        void ReadFrameEncoding(Stream transportStream, IRenderTarget? renderTarget, in Rectangle rectangle, in Size framebufferSize, in PixelFormat framebufferFormat);
    }
}
