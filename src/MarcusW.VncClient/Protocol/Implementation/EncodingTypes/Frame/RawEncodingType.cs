using System.IO;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Frame
{
    /// <summary>
    /// A frame encoding type for raw pixel data.
    /// </summary>
    public class RawEncodingType : FrameEncodingType
    {
        /// <inheritdoc />
        public override int Id => 0;

        /// <inheritdoc />
        public override string Name => "Raw";

        /// <inheritdoc />
        public override int Priority => 1;

        /// <inheritdoc />
        public override bool GetsConfirmed => false; // All servers support this encoding type.

        /// <inheritdoc />
        public override void ReadFrameEncoding(Stream transportStream, IRenderTarget? renderTarget, in Rectangle rectangle, in Size framebufferSize,
            in PixelFormat framebufferFormat)
        {

        }
    }
}
