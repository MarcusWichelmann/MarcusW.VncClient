using System;
using System.Threading;
using MarcusW.VncClient.Protocol.Services;
using TurboJpegWrapper;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Communication
{
    /// <inhertitdoc />
    public sealed class ImageDecoder : IImageDecoder
    {
        // Please note that the JPEG PixelFormat names refer to the byte order in memory (LE). Therefore the shifting has to be reversed.
        private static readonly PixelFormat RgbaCompatiblePixelFormat = new PixelFormat("JPEG Compatible RGBA", 32, 32, false, true, true, 255, 255, 255, 255, 24-24, 24-16, 24-8, 24-0);
        private static readonly PixelFormat BgraCompatiblePixelFormat = new PixelFormat("JPEG Compatible BGRA", 32, 32, false, true, true, 255, 255, 255, 255, 24-8, 24-16, 24-24, 24-0);
        private static readonly PixelFormat ArgbCompatiblePixelFormat = new PixelFormat("JPEG Compatible ARGB", 32, 32, false, true, true, 255, 255, 255, 255, 24-16, 24-8, 24-0, 24-24);
        private static readonly PixelFormat AbgrCompatiblePixelFormat = new PixelFormat("JPEG Compatible ABGR", 32, 32, false, true, true, 255, 255, 255, 255, 24-0, 24-8, 24-16, 24-24);

        private readonly TJDecompressor _jpegDecompressor = new TJDecompressor();

        private volatile bool _disposed;

        /// <inhertitdoc />
        public void DecodeJpegTo32Bit(Span<byte> jpegBuffer, Span<byte> pixelsBuffer, PixelFormat preferredPixelFormat, out PixelFormat usedPixelFormat,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tjPixelFormat = TJPixelFormat.RGBA;
            usedPixelFormat = RgbaCompatiblePixelFormat;

            if (BgraCompatiblePixelFormat.IsBinaryCompatibleTo(preferredPixelFormat))
            {
                tjPixelFormat = TJPixelFormat.BGRA;
                usedPixelFormat = BgraCompatiblePixelFormat;
            }
            else if (ArgbCompatiblePixelFormat.IsBinaryCompatibleTo(preferredPixelFormat))
            {
                tjPixelFormat = TJPixelFormat.ARGB;
                usedPixelFormat = ArgbCompatiblePixelFormat;
            }
            else if (AbgrCompatiblePixelFormat.IsBinaryCompatibleTo(preferredPixelFormat))
            {
                tjPixelFormat = TJPixelFormat.ABGR;
                usedPixelFormat = AbgrCompatiblePixelFormat;
            }

            _jpegDecompressor.Decompress(jpegBuffer, pixelsBuffer, tjPixelFormat, TJFlags.FastUpsample | TJFlags.FastDct | TJFlags.NoRealloc, out int _, out int _, out int _);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _jpegDecompressor.Dispose();

            _disposed = true;
        }
    }
}
