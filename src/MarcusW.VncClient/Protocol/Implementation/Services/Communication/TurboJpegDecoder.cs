using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using MarcusW.VncClient.Protocol.Implementation.Native;
using MarcusW.VncClient.Protocol.Services;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Communication
{
    /// <summary>
    /// A <see cref="IJpegDecoder"/> implementation using the TurboJPEG library.
    /// Provides methods for decoding JPEG images.
    /// </summary>
    public sealed class TurboJpegDecoder : IJpegDecoder
    {
        // Please note that the JPEG PixelFormat names refer to the byte order in memory (LE). Therefore the shifting has to be reversed.
        private static readonly PixelFormat RgbaCompatiblePixelFormat =
            new PixelFormat("JPEG Compatible RGBA", 32, 32, false, true, true, 255, 255, 255, 255, 24 - 24, 24 - 16, 24 - 8, 24 - 0);

        private static readonly PixelFormat BgraCompatiblePixelFormat =
            new PixelFormat("JPEG Compatible BGRA", 32, 32, false, true, true, 255, 255, 255, 255, 24 - 8, 24 - 16, 24 - 24, 24 - 0);

        private static readonly PixelFormat ArgbCompatiblePixelFormat =
            new PixelFormat("JPEG Compatible ARGB", 32, 32, false, true, true, 255, 255, 255, 255, 24 - 16, 24 - 8, 24 - 0, 24 - 24);

        private static readonly PixelFormat AbgrCompatiblePixelFormat =
            new PixelFormat("JPEG Compatible ABGR", 32, 32, false, true, true, 255, 255, 255, 255, 24 - 0, 24 - 8, 24 - 16, 24 - 24);

        /// <summary>
        /// Gets whether this decoder is available on the current system.
        /// </summary>
        public static bool IsAvailable => TurboJpeg.IsAvailable;

        private IntPtr _decompressorHandle;

        private volatile bool _disposed;

        /// <inhertitdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DecodeJpegTo32Bit(Span<byte> jpegBuffer, Span<byte> pixelsBuffer, int expectedWidth, int expectedHeight, PixelFormat preferredPixelFormat,
            out PixelFormat usedPixelFormat, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TurboJpegDecoder));

            if (!TurboJpeg.IsAvailable)
                throw new InvalidOperationException("The TurboJPEG decoder is unavailable on the current system.");

            // Initialize on first use
            if (_decompressorHandle == IntPtr.Zero)
                Initialize();

            cancellationToken.ThrowIfCancellationRequested();

            int jpegBufferLength = jpegBuffer.Length;
            int pixelsBufferLength = pixelsBuffer.Length;

            // NOTE: We only use 32bit pixel formats, so we don't have to deal with padding here.

            // Find a fitting pixel format that requires the least conversion later
            var tjPixelFormat = TurboJpegPixelFormat.RGBA;
            usedPixelFormat = RgbaCompatiblePixelFormat;
            if (BgraCompatiblePixelFormat.IsBinaryCompatibleTo(preferredPixelFormat))
            {
                tjPixelFormat = TurboJpegPixelFormat.BGRA;
                usedPixelFormat = BgraCompatiblePixelFormat;
            }
            else if (ArgbCompatiblePixelFormat.IsBinaryCompatibleTo(preferredPixelFormat))
            {
                tjPixelFormat = TurboJpegPixelFormat.ARGB;
                usedPixelFormat = ArgbCompatiblePixelFormat;
            }
            else if (AbgrCompatiblePixelFormat.IsBinaryCompatibleTo(preferredPixelFormat))
            {
                tjPixelFormat = TurboJpegPixelFormat.ABGR;
                usedPixelFormat = AbgrCompatiblePixelFormat;
            }

            fixed (byte* jpegBufferPtr = jpegBuffer)
            fixed (byte* pixelsBufferPtr = pixelsBuffer)
            {
                // Retrieve the JPEG header information so we can make sure, that our buffer is large enough
                if (TurboJpeg.DecompressHeader(_decompressorHandle, (IntPtr)jpegBufferPtr, (ulong)jpegBufferLength, out int width, out int height, out _, out _) == -1)
                    throw new RfbProtocolException($"Decompressiong JPEG header failed: {TurboJpeg.GetLastError()}");

                // Validate the image size
                if (width != expectedWidth || height != expectedHeight)
                    throw new RfbProtocolException(
                        $"Cannot decode JPEG image because it's size of {width}x{height} does not match the expected size {expectedWidth}x{expectedHeight}.");

                // Validate the buffer size
                int stride = width * 4;
                int requiredBufferLength = stride * height;
                if (pixelsBufferLength < requiredBufferLength)
                    throw new RfbProtocolException(
                        $"Cannot decode JPEG image ({width}x{height}) because it's size of {requiredBufferLength} bytes would exceed the pixels buffer size of {pixelsBufferLength} when decompressing. ");

                Debug.Assert(pixelsBufferLength == requiredBufferLength, "pixelsBufferLength == requiredBufferLength");

                // Decompress the image to the pixels buffer
                if (TurboJpeg.Decompress(_decompressorHandle, (IntPtr)jpegBufferPtr, (ulong)jpegBufferLength, (IntPtr)pixelsBufferPtr, width, stride, height, (int)tjPixelFormat,
                    (int)(TurboJpegFlags.FastUpsample | TurboJpegFlags.FastDct | TurboJpegFlags.NoRealloc)) == -1)
                    throw new RfbProtocolException($"Decompressiong JPEG image failed: {TurboJpeg.GetLastError()}");
            }
        }

        private void Initialize()
        {
            if (_decompressorHandle != IntPtr.Zero)
                return;

            _decompressorHandle = TurboJpeg.InitDecompressorInstance();
            if (_decompressorHandle == IntPtr.Zero)
                throw new RfbProtocolException($"Initializing TurboJPEG decompressor instance failed: {TurboJpeg.GetLastError()}");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);

            _disposed = true;
        }

        /// <inheritdoc />
        ~TurboJpegDecoder()
        {
            ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
            if (_decompressorHandle != IntPtr.Zero)
            {
                TurboJpeg.DestroyInstance(_decompressorHandle);
                _decompressorHandle = IntPtr.Zero;
            }
        }
    }
}
