using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Frame
{
    /// <summary>
    /// Base class for <see cref="IFrameEncodingType"/> implementations.
    /// </summary>
    public abstract class FrameEncodingType : IFrameEncodingType
    {
        /// <inheritdoc />
        public abstract int Id { get; }

        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public abstract int Priority { get; }

        /// <inheritdoc />
        public abstract bool GetsConfirmed { get; }

        /// <inheritdoc />
        public abstract void ReadFrameEncoding(Stream transportStream, IRenderTarget? renderTarget, in Rectangle rectangle, in Size framebufferSize,
            in PixelFormat framebufferFormat);
    }
}
