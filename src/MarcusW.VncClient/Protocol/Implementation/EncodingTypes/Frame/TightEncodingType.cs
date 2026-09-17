using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.Services;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Frame
{
    /// <summary>
    /// A frame encoding type for Tight compressed pixel data.
    /// </summary>
    public class TightEncodingType : FrameEncodingType
    {
        private readonly RfbConnectionContext _context;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.Tight;

        /// <inheritdoc />
        public override string Name => "Tight";

        /// <inheritdoc />
        public override int Priority => 300;

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <inheritdoc />
        public override Color VisualizationColor => new Color(0, 255, 255);

        /// <summary>
        /// Initializes a new instance of the <see cref="TightEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public TightEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public override void ReadFrameEncoding(Stream transportStream, IFramebufferReference? targetFramebuffer, in Rectangle rectangle, in Size remoteFramebufferSize,
            in PixelFormat remoteFramebufferFormat)
        {
            if (transportStream == null)
                throw new ArgumentNullException(nameof(transportStream));

            Debug.Assert(_context.ZLibInflater != null, "_context.ZLibInflater != null");
            IZLibInflater zlibInflater = _context.ZLibInflater;

            // Read compression control byte
            Span<byte> compressionControlBuffer = stackalloc byte[1];
            if (transportStream.Read(compressionControlBuffer) == 0)
                throw new UnexpectedEndOfStreamException("Stream reached its end while reading Tight compression control.");
            byte compressionControl = compressionControlBuffer[0];

            // Reset zlib streams as requested
            for (var i = 0; i < 4; i++)
            {
                if (((compressionControl >> i) & 1) != 0)
                    zlibInflater.ResetZlibStream(i);
            }

            // Decide on format for tpixels (Tight pixels)
            PixelFormat tPixelFormat = GetTPixelFormat(remoteFramebufferFormat);

            // Create a cursor for the target framebuffer, if any framebuffer reference is available
            bool hasTargetFramebuffer = targetFramebuffer != null;
            FramebufferCursor framebufferCursor = hasTargetFramebuffer ? new FramebufferCursor(targetFramebuffer!, rectangle,
                remoteFramebufferFormat.TrueColor ? null : _context.GetState<ProtocolState>().RemoteFramebufferColorMap) : default;

            // Get the compression type
            if ((compressionControl & 128) == 0) // Basic compression
            {
                int zlibStreamId = (compressionControl >> 4) & 0b11;
                bool readFilterId = (compressionControl & 64) != 0;

                ReadBasicCompressedRectangle(transportStream, hasTargetFramebuffer, ref framebufferCursor, rectangle, tPixelFormat, zlibStreamId, readFilterId);
            }
            else if ((compressionControl & 16) == 0) // Fill compression
            {
                ReadFillCompressedRectangle(transportStream, hasTargetFramebuffer, ref framebufferCursor, rectangle, tPixelFormat);
            }
            else // Jpeg compression
            {
                if (remoteFramebufferFormat.BitsPerPixel != 16 && remoteFramebufferFormat.BitsPerPixel != 32)
                    throw new UnexpectedDataException("Tight JPEG compression is not supported for bpp values other than 16 and 32.");

                ReadJpegCompressedRectangle(transportStream, hasTargetFramebuffer, ref framebufferCursor, rectangle);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PixelFormat GetTPixelFormat(in PixelFormat remoteFramebufferFormat)
        {
            if (remoteFramebufferFormat.TrueColor && remoteFramebufferFormat.BitsPerPixel == 32 && remoteFramebufferFormat.Depth == 24 && !remoteFramebufferFormat.HasAlpha
                && remoteFramebufferFormat.RedMax == 255 && remoteFramebufferFormat.GreenMax == 255 && remoteFramebufferFormat.BlueMax == 255)
            {
                // Received as R,G,B --> memory(LE): RGB0 (0x0BGR)
                return new PixelFormat(remoteFramebufferFormat.Name, 24, 24, false, true, false, 255, 255, 255, 0, 0, 8, 16, 0);
            }

            return remoteFramebufferFormat;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private void ReadBasicCompressedRectangle(Stream stream, bool hasTargetFramebuffer, ref FramebufferCursor framebufferCursor, in Rectangle rectangle,
            in PixelFormat tPixelFormat, int zlibStreamId, bool readFilterId)
        {
            // Read the filter id (CopyFilter is the default)
            var filterId = 0;
            if (readFilterId)
            {
                Span<byte> filterIdBuffer = stackalloc byte[1];
                if (stream.Read(filterIdBuffer) == 0)
                    throw new UnexpectedEndOfStreamException("Stream reached its end while reading Tight basic compression filter id.");
                filterId = filterIdBuffer[0];
            }

            switch (filterId)
            {
                case 0: // CopyFilter (Raw)
                    ReadBasicCompressedCopyFilterRectangle(stream, hasTargetFramebuffer, ref framebufferCursor, rectangle, tPixelFormat, zlibStreamId);
                    break;

                case 1: // PaletteFilter
                    ReadBasicCompressedPaletteFilterRectangle(stream, hasTargetFramebuffer, ref framebufferCursor, rectangle, tPixelFormat, zlibStreamId);
                    break;

                case 2: // GradientFilter
                    if (tPixelFormat.BitsPerPixel != 16 && tPixelFormat.BitsPerPixel != 32)
                        throw new UnsupportedProtocolFeatureException("Tight gradient filter is only supported for 16 and 32 bpp.");
                    ReadBasicCompressedGradientFilterRectangle(stream, hasTargetFramebuffer, ref framebufferCursor, rectangle, tPixelFormat, zlibStreamId);
                    break;

                default: throw new UnexpectedDataException($"Tight basic compression filter id of {filterId} is invalid.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private void ReadBasicCompressedCopyFilterRectangle(Stream stream, bool hasTargetFramebuffer, ref FramebufferCursor framebufferCursor, in Rectangle rectangle,
            in PixelFormat tPixelFormat, int zlibStreamId)
        {
            int bytesPerPixel = tPixelFormat.BytesPerPixel;
            int pixelsCount = rectangle.Size.Width * rectangle.Size.Height;
            int pixelsDataLength = pixelsCount * bytesPerPixel;

            // Get pixel data stream
            Stream pixelDataStream = GetBasicCompressedPixelDataStream(stream, pixelsDataLength, zlibStreamId);

            // Skip all pixel data if there is nothing to render to.
            if (!hasTargetFramebuffer)
            {
                pixelDataStream.SkipAll(pixelsDataLength);
                return;
            }

            // Rent a buffer for the raw pixel data.
            // This is fine because we either read from the zlib stream which operates on a memory buffer, anyway, or we read only very few bytes (<12) at once.
            byte[] pixelsBuffer = ArrayPool<byte>.Shared.Rent(pixelsDataLength);
            Span<byte> pixelsSpan = pixelsBuffer.AsSpan().Slice(0, pixelsDataLength);
            try
            {
                // Read pixel data
                pixelDataStream.ReadAll(pixelsSpan);

                // Draw the pixels
                unsafe
                {
                    fixed (byte* pixelsPtr = pixelsSpan)
                    {
                        for (var processedBytes = 0; processedBytes < pixelsDataLength; processedBytes += bytesPerPixel)
                        {
                            // Set the pixel
                            framebufferCursor.SetPixel(pixelsPtr + processedBytes, tPixelFormat);
                            if (!framebufferCursor.GetEndReached())
                                framebufferCursor.MoveNext();
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pixelsBuffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private void ReadBasicCompressedPaletteFilterRectangle(Stream stream, bool hasTargetFramebuffer, ref FramebufferCursor framebufferCursor, in Rectangle rectangle,
            in PixelFormat tPixelFormat, int zlibStreamId)
        {
            int bytesPerPixel = tPixelFormat.BytesPerPixel;

            // Read palette size
            Span<byte> paletteSizeBuffer = stackalloc byte[1];
            if (stream.Read(paletteSizeBuffer) == 0)
                throw new UnexpectedEndOfStreamException("Stream reached its end while reading Tight basic compression palette size.");
            int paletteSize = paletteSizeBuffer[0] + 1;

            // For a palette size of 2 only a single bit is used to encode each pixel
            bool isSingleBitEncoded = paletteSize <= 2;
            int bitsPerPixel = isSingleBitEncoded ? 1 : 8;
            int indexMask = isSingleBitEncoded ? 1 : 255;

            // Rent a buffer that fits the palette and the pixel data (won't get very large, anyway)
            int paletteDataLength = paletteSize * bytesPerPixel;
            int pixelsDataLength = isSingleBitEncoded ? (rectangle.Size.Width + 7) / 8 * rectangle.Size.Height : rectangle.Size.Width * rectangle.Size.Height;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(paletteDataLength + pixelsDataLength);
            Span<byte> bufferSpan = buffer.AsSpan();
            Span<byte> paletteSpan = bufferSpan.Slice(0, paletteDataLength);
            Span<byte> pixelsSpan = bufferSpan.Slice(paletteDataLength, pixelsDataLength);
            try
            {
                // Read the color palette
                stream.ReadAll(paletteSpan);

                // Get pixel data stream
                Stream pixelDataStream = GetBasicCompressedPixelDataStream(stream, pixelsDataLength, zlibStreamId);

                // Skip all pixel data if there is nothing to render to.
                if (!hasTargetFramebuffer)
                {
                    pixelDataStream.SkipAll(pixelsDataLength);
                    return;
                }

                // Read pixel data
                pixelDataStream.ReadAll(pixelsSpan);

                // Draw the pixels
                unsafe
                {
                    fixed (byte* palettePtr = paletteSpan)
                    fixed (byte* pixelsPtr = pixelsSpan)
                    {
                        byte* nextPixelsBytePtr = pixelsPtr;

                        // Process the pixels line by line
                        for (var i = 0; i < rectangle.Size.Height; i++)
                        {
                            byte pixelsByte = 0;
                            var remainingBits = 0;

                            // Write pixels left to right
                            for (var j = 0; j < rectangle.Size.Width; j++)
                            {
                                // Read next byte?
                                if (remainingBits == 0)
                                {
                                    pixelsByte = *nextPixelsBytePtr++;
                                    remainingBits = 8;
                                }

                                // Get palette index
                                remainingBits -= bitsPerPixel;
                                int paletteIndex = ((pixelsByte >> remainingBits) & indexMask) * bytesPerPixel;
                                if (paletteIndex >= paletteDataLength)
                                    throw new UnexpectedDataException(
                                        $"Received invalid Tight basic compressed palette index of {paletteIndex} for a palette of {paletteDataLength} bytes.");

                                // Set the pixel
                                framebufferCursor.SetPixel(palettePtr + paletteIndex, tPixelFormat);
                                if (!framebufferCursor.GetEndReached())
                                    framebufferCursor.MoveNext();
                            }
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private void ReadBasicCompressedGradientFilterRectangle(Stream stream, bool hasTargetFramebuffer, ref FramebufferCursor framebufferCursor, in Rectangle rectangle,
            in PixelFormat tPixelFormat, int zlibStreamId)
        {
            int bytesPerPixel = tPixelFormat.BytesPerPixel;
            int width = rectangle.Size.Width;
            int height = rectangle.Size.Height;
            int pixelsDataLength = width * height * bytesPerPixel;

            // Get pixel data stream
            Stream pixelDataStream = GetBasicCompressedPixelDataStream(stream, pixelsDataLength, zlibStreamId);

            // Skip all pixel data if there is nothing to render to.
            if (!hasTargetFramebuffer)
            {
                pixelDataStream.SkipAll(pixelsDataLength);
                return;
            }

            // Rent buffers for the compressed data and a row of previous pixel values
            byte[] pixelsBuffer = ArrayPool<byte>.Shared.Rent(pixelsDataLength);
            byte[] prevRowBuffer = ArrayPool<byte>.Shared.Rent(width * bytesPerPixel);
            byte[] currentRowBuffer = ArrayPool<byte>.Shared.Rent(width * bytesPerPixel);
            Span<byte> pixelsSpan = pixelsBuffer.AsSpan().Slice(0, pixelsDataLength);
            Span<byte> prevRow = prevRowBuffer.AsSpan().Slice(0, width * bytesPerPixel);
            Span<byte> currentRow = currentRowBuffer.AsSpan().Slice(0, width * bytesPerPixel);

            try
            {
                // Read all gradient-encoded pixel data
                pixelDataStream.ReadAll(pixelsSpan);

                // Initialize previous row to zero
                prevRow.Clear();

                // Get max values for each component
                int redMax = tPixelFormat.RedMax;
                int greenMax = tPixelFormat.GreenMax;
                int blueMax = tPixelFormat.BlueMax;

                // Process row by row
                int pixelIndex = 0;
                for (int y = 0; y < height; y++)
                {
                    // Initialize previous pixel in current row to zero
                    int prevR = 0, prevG = 0, prevB = 0;
                    int prevAboveR = 0, prevAboveG = 0, prevAboveB = 0;

                    for (int x = 0; x < width; x++)
                    {
                        // Read the gradient-encoded differences
                        int diffR, diffG, diffB;
                        if (bytesPerPixel == 3)
                        {
                            diffR = pixelsSpan[pixelIndex];
                            diffG = pixelsSpan[pixelIndex + 1];
                            diffB = pixelsSpan[pixelIndex + 2];
                        }
                        else // bytesPerPixel == 4
                        {
                            // For 32bpp TPIXEL format (R, G, B order)
                            diffR = pixelsSpan[pixelIndex];
                            diffG = pixelsSpan[pixelIndex + 1];
                            diffB = pixelsSpan[pixelIndex + 2];
                        }

                        // Get the pixel above (from previous row)
                        int aboveR = 0, aboveG = 0, aboveB = 0;
                        if (y > 0)
                        {
                            int aboveIndex = x * bytesPerPixel;
                            aboveR = prevRow[aboveIndex];
                            aboveG = prevRow[aboveIndex + 1];
                            aboveB = prevRow[aboveIndex + 2];
                        }

                        // Calculate the predicted value: P[i,j] = V[i-1,j] + V[i,j-1] - V[i-1,j-1]
                        // Clamp to [0, MAX]
                        int predictedR = Math.Clamp(prevR + aboveR - prevAboveR, 0, redMax);
                        int predictedG = Math.Clamp(prevG + aboveG - prevAboveG, 0, greenMax);
                        int predictedB = Math.Clamp(prevB + aboveB - prevAboveB, 0, blueMax);

                        // Recover the original value: V[i,j] = D[i,j] + P[i,j]
                        int r = (diffR + predictedR) & redMax;
                        int g = (diffG + predictedG) & greenMax;
                        int b = (diffB + predictedB) & blueMax;

                        // Store in current row buffer
                        int currentIndex = x * bytesPerPixel;
                        currentRow[currentIndex] = (byte)r;
                        currentRow[currentIndex + 1] = (byte)g;
                        currentRow[currentIndex + 2] = (byte)b;
                        if (bytesPerPixel == 4)
                            currentRow[currentIndex + 3] = 0;

                        // Update previous values for next iteration
                        prevAboveR = aboveR;
                        prevAboveG = aboveG;
                        prevAboveB = aboveB;
                        prevR = r;
                        prevG = g;
                        prevB = b;

                        pixelIndex += bytesPerPixel;
                    }

                    // Write the decoded row to framebuffer
                    unsafe
                    {
                        fixed (byte* rowPtr = currentRow)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                framebufferCursor.SetPixel(rowPtr + x * bytesPerPixel, tPixelFormat);
                                if (!framebufferCursor.GetEndReached())
                                    framebufferCursor.MoveNext();
                            }
                        }
                    }

                    // Swap row buffers (can't use tuple swap with Span<byte>)
                    var tempRow = prevRow;
                    prevRow = currentRow;
                    currentRow = tempRow;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pixelsBuffer);
                ArrayPool<byte>.Shared.Return(prevRowBuffer);
                ArrayPool<byte>.Shared.Return(currentRowBuffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private Stream GetBasicCompressedPixelDataStream(Stream baseStream, int expectedLength, int zlibStreamId)
        {
            // No zlib compression is used for small data chunks
            if (expectedLength < 12)
                return baseStream;

            // Read length of zlib data
            var zlibDataLength = (int)ReadCompactNumber(baseStream);

            // Create and return a stream for inflating the data
            Debug.Assert(_context.ZLibInflater != null, "_context.ZLibInflater != null");
            return _context.ZLibInflater.ReadAndInflate(baseStream, zlibDataLength, zlibStreamId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private void ReadFillCompressedRectangle(Stream stream, bool hasTargetFramebuffer, ref FramebufferCursor framebufferCursor, in Rectangle rectangle,
            in PixelFormat tPixelFormat)
        {
            // Read a single color value
            Span<byte> pixelBuffer = stackalloc byte[tPixelFormat.BytesPerPixel];
            stream.ReadAll(pixelBuffer);

            // Skip rendering if there is nothing to render to.
            if (!hasTargetFramebuffer)
                return;

            // Fill the tile with a solid color
            unsafe
            {
                fixed (byte* bufferPtr = pixelBuffer)
                    framebufferCursor.SetPixelsSolid(bufferPtr, tPixelFormat, rectangle.Size.Width * rectangle.Size.Height);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private void ReadJpegCompressedRectangle(Stream stream, bool hasTargetFramebuffer, ref FramebufferCursor framebufferCursor, in Rectangle rectangle)
        {
            Debug.Assert(_context.JpegDecoder != null, "_context.JpegDecoder != null");
            IJpegDecoder jpegDecoder = _context.JpegDecoder;

            // Read length of compressed data
            var jpegDataLength = (int)ReadCompactNumber(stream);

            // Skip all the following if there is nothing to render to
            if (!hasTargetFramebuffer)
            {
                stream.SkipAll(jpegDataLength);
                return;
            }

            // Size of the decompressed 32bit image
            int pixelsCount = rectangle.Size.Width * rectangle.Size.Height;
            int pixelsDataLength = pixelsCount * 4;

            // Rent buffers
            byte[] jpegBuffer = ArrayPool<byte>.Shared.Rent(jpegDataLength);
            byte[] pixelsBuffer = ArrayPool<byte>.Shared.Rent(pixelsDataLength);
            Span<byte> jpegSpan = jpegBuffer.AsSpan().Slice(0, jpegDataLength);
            Span<byte> pixelsSpan = pixelsBuffer.AsSpan().Slice(0, pixelsDataLength);
            try
            {
                // Read compressed data
                stream.ReadAll(jpegSpan);

                // Decompress image to a 32bit pixel format that's compatible to the framebuffer format, if possible.
                jpegDecoder.DecodeJpegTo32Bit(jpegSpan, pixelsSpan, rectangle.Size.Width, rectangle.Size.Height, framebufferCursor.FramebufferFormat, out PixelFormat pixelFormat);

                // Write pixels to the target framebuffer
                unsafe
                {
                    fixed (byte* pixelsPtr = pixelsSpan)
                        framebufferCursor.SetPixels(pixelsPtr, pixelFormat, pixelsCount);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(jpegBuffer);
                ArrayPool<byte>.Shared.Return(pixelsBuffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private uint ReadCompactNumber(Stream stream)
        {
            uint number = 0; // 0x zzzzzzzz yyyyyyy xxxxxxx

            Span<byte> byteBuffer = stackalloc byte[1];
            var shift = 0;
            while (true)
            {
                if (stream.Read(byteBuffer) == 0)
                    throw new UnexpectedEndOfStreamException("Stream reached its end while reading a compact number representation.");
                byte b = byteBuffer[0];

                number |= (uint)(b << shift);

                // Last run?
                if (shift == 14 || (b & 128) == 0)
                    break;

                shift += 7;

                // Strip highest bit
                number &= (uint)~(1 << shift);
            }

            return number;
        }
    }
}
