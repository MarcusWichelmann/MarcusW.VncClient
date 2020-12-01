using System;
using System.Threading;

namespace MarcusW.VncClient.Protocol.Services
{
    /// <summary>
    /// Provides methods for decoding JPEG images.
    /// </summary>
    /// <remarks>
    /// It's not necessary that implementations of this interface are thread-safe because the processing of received frames always happens synchronously in a single thread.
    /// Because of this, calls to methods of such implementations should never come from multiple threads.
    /// </remarks>
    public interface IJpegDecoder : IDisposable
    {
        /// <summary>
        /// Decompresses a JPEG encoded image to a 32bit <see cref="pixelsBuffer"/>.
        /// </summary>
        /// <param name="jpegBuffer">The compressed data.</param>
        /// <param name="pixelsBuffer">The 32bit target buffer.</param>
        /// <param name="expectedWidth">The expected width of the decompressed image.</param>
        /// <param name="expectedHeight">The expected height of the decompressed image.</param>
        /// <param name="preferredPixelFormat">The preferred 32bit target pixel format.</param>
        /// <param name="usedPixelFormat">The pixel format that was chosen for the pixels written to the <see cref="pixelsBuffer"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void DecodeJpegTo32Bit(Span<byte> jpegBuffer, Span<byte> pixelsBuffer, int expectedWidth, int expectedHeight, PixelFormat preferredPixelFormat,
            out PixelFormat usedPixelFormat, CancellationToken cancellationToken = default);
    }
}
