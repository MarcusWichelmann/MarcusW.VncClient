using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Frame
{
    /// <summary>
    /// A frame encoding type for ZRLE compressed pixel data.
    /// </summary>
    public class ZrleEncodingType : FrameEncodingType
    {
        private const int TileSize = 64;

        private readonly RfbConnectionContext _context;

        // A buffer which fits even the largest tiles
        private readonly byte[] _tileBuffer = new byte[TileSize * TileSize * 4];

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.ZRLE;

        /// <inheritdoc />
        public override string Name => "ZRLE";

        /// <inheritdoc />
        public override int Priority => 100;

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <inheritdoc />
        public override Color VisualizationColor => new Color(0, 255, 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="ZrleEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public ZrleEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        public override void ReadFrameEncoding(Stream transportStream, IFramebufferReference? targetFramebuffer, in Rectangle rectangle, in Size remoteFramebufferSize,
            in PixelFormat remoteFramebufferFormat)
        {
            if (transportStream == null)
                throw new ArgumentNullException(nameof(transportStream));

            // Read header with data length
            Span<byte> header = stackalloc byte[4];
            transportStream.ReadAll(header);
            uint dataLength = BinaryPrimitives.ReadUInt32BigEndian(header);

            // Create stream for inflating the data
            Debug.Assert(_context.ZLibInflater != null, "_context.ZLibInflater != null");
            Stream inflateStream = _context.ZLibInflater.ReadAndInflate(transportStream, (int)dataLength);

            // Decide on format for cpixels (compressed pixels) while respecting some special cases
            PixelFormat cPixelFormat = GetCPixelFormat(remoteFramebufferFormat);

            // Calculate additional rectangle dimensions
            int rectRightX = rectangle.Position.X + rectangle.Size.Width;
            int rectBottomY = rectangle.Position.Y + rectangle.Size.Height;

            // Iterate over all tiles
            for (int tileY = rectangle.Position.Y; tileY < rectBottomY; tileY += TileSize)
            {
                int tileHeight = Math.Min(rectBottomY - tileY, TileSize);

                for (int tileX = rectangle.Position.X; tileX < rectRightX; tileX += TileSize)
                {
                    int tileWidth = Math.Min(rectRightX - tileX, TileSize);

                    // Read tile
                    ReadTile(inflateStream, targetFramebuffer, new Rectangle(tileX, tileY, tileWidth, tileHeight), cPixelFormat);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PixelFormat GetCPixelFormat(in PixelFormat remoteFramebufferFormat)
        {
            // See https://github.com/TigerVNC/tigervnc/blob/d8bbbeb3b37c713a72a113f7ef78741e15cc4a4d/common/rfb/ZRLEDecoder.cxx#L84

            if (remoteFramebufferFormat.TrueColor && remoteFramebufferFormat.BitsPerPixel == 32 && remoteFramebufferFormat.Depth <= 24 && !remoteFramebufferFormat.HasAlpha)
            {
                // Create a white pixel using the given pixel format
                var maxPixel = (uint)((remoteFramebufferFormat.RedMax << remoteFramebufferFormat.RedShift)
                    | (remoteFramebufferFormat.GreenMax << remoteFramebufferFormat.GreenShift) | (remoteFramebufferFormat.BlueMax << remoteFramebufferFormat.BlueShift));

                // Does the white pixel fit in the least/most significant 3 bytes (big-endian view)?
                bool fitsInLs3Bytes = maxPixel < 1 << 24;
                bool fitsInMs3Bytes = (maxPixel & 0xff) == 0;

                // Note that we have to differentiate between endianness here, because reversing the bytes also affects,
                // where we have to put the received bytes when reconstructing the pixel value:

                // Should the received bytes be put first in memory, when reconstructing the pixel value? memory(LE): ___0 (0x0___)
                // little-endian received as C,B,A --> memory(LE): CBA0 (0x0ABC)                     == least-significant
                //    big-endian received as A,B,C --> memory(LE): ABC0 (0x0CBA) --> reverse: 0xABC0 ==  most-significant
                if ((fitsInLs3Bytes && remoteFramebufferFormat.LittleEndian) || (fitsInMs3Bytes && remoteFramebufferFormat.BigEndian))
                {
                    // The pixel conversion algorithm automatically puts the three bytes first in memory (with 1 trash byte after), but we know,
                    // that only the relevant bytes are used because of the correct shifting.
                    return new PixelFormat(remoteFramebufferFormat.Name, 24, remoteFramebufferFormat.Depth, remoteFramebufferFormat.BigEndian, true, false,
                        remoteFramebufferFormat.RedMax, remoteFramebufferFormat.GreenMax, remoteFramebufferFormat.BlueMax, 0, remoteFramebufferFormat.RedShift,
                        remoteFramebufferFormat.GreenShift, remoteFramebufferFormat.BlueShift, 0);
                }

                // Should the received bytes be put last in memory, when reconstructing the pixel value? memory(LE): 0___ (0x___0)
                //    big-endian received as A,B,C --> memory(LE): 0ABC (0xCBA0) --> reverse: 0x0ABC == least-significant
                // little-endian received as C,B,A --> memory(LE): 0CBA (0xABC0)                     == most-significant
                if ((fitsInLs3Bytes && remoteFramebufferFormat.BigEndian) || (fitsInMs3Bytes && remoteFramebufferFormat.LittleEndian))
                {
                    // The pixel conversion algorithm automatically puts the three bytes first in memory (with 1 trash byte after), which is not what we want:
                    //    big-endian received as A,B,C --> memory(LE): ABC0 (0x0CBA) --> reverse: 0xABC0  SHOULD BE  0x0ABC == least-significant
                    // little-endian received as C,B,A --> memory(LE): CBA0 (0x0ABC)                      SHOULD BE  0xABC0 == most-significant
                    // To fix this, we can add/subtract 8 to/from the shift values, to make them read the correct bits again.
                    int shiftOffset = fitsInLs3Bytes ? 8 : -8;
                    return new PixelFormat(remoteFramebufferFormat.Name, 24, remoteFramebufferFormat.Depth, remoteFramebufferFormat.BigEndian, true, false,
                        remoteFramebufferFormat.RedMax, remoteFramebufferFormat.GreenMax, remoteFramebufferFormat.BlueMax, 0,
                        (byte)(remoteFramebufferFormat.RedShift + shiftOffset), (byte)(remoteFramebufferFormat.GreenShift + shiftOffset),
                        (byte)(remoteFramebufferFormat.BlueShift + shiftOffset), 0);
                }
            }

            return remoteFramebufferFormat;
        }

#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        private void ReadTile(Stream stream, IFramebufferReference? targetFramebuffer, in Rectangle tile, in PixelFormat cPixelFormat)
        {
            // Read one byte for the subencoding type
            Span<byte> subencodingTypeBuffer = stackalloc byte[1];

            if (stream.Read(subencodingTypeBuffer) == 0)
                throw new UnexpectedEndOfStreamException("Stream reached its end while reading tile subencoding type.");
            byte subencodingType = subencodingTypeBuffer[0];

            // Top bit defines if this tile is run-length encoded, bottom 7 bits define the palette size
            bool isRunLengthEncoded = (subencodingType & 128) != 0;
            int paletteSize = subencodingType & 127;

            // Create a cursor for this tile on the target framebuffer, if any framebuffer reference is available
            bool hasTargetFramebuffer = targetFramebuffer != null;
            FramebufferCursor framebufferCursor = hasTargetFramebuffer ? new FramebufferCursor(targetFramebuffer!, tile) : default;

            // Read tile based on the subencoding type
            if (!isRunLengthEncoded)
            {
                switch (paletteSize)
                {
                    // Raw
                    case 0:
                        ReadRawTile(stream, hasTargetFramebuffer, ref framebufferCursor, tile, cPixelFormat);
                        break;

                    // Solid color
                    case 1:
                        ReadSolidTile(stream, hasTargetFramebuffer, ref framebufferCursor, tile, cPixelFormat);
                        break;

                    // Packed palette
                    case var _ when paletteSize >= 2 && paletteSize <= 16:
                        ReadPackedPaletteTile(stream, hasTargetFramebuffer, ref framebufferCursor, tile, cPixelFormat, paletteSize);
                        break;

                    default: throw new UnexpectedDataException($"Received unexpected palette size: {paletteSize}");
                }
            }
            else
            {
                switch (paletteSize)
                {
                    // Plain RLE
                    case 0:
                        ReadRleTile(stream, hasTargetFramebuffer, ref framebufferCursor, tile, cPixelFormat);
                        break;

                    case var _ when paletteSize >= 2 && paletteSize <= 127:
                        ReadPaletteRleTile(stream, hasTargetFramebuffer, ref framebufferCursor, tile, cPixelFormat, paletteSize);
                        break;

                    default: throw new UnexpectedDataException($"Received unexpected RLE palette size: {paletteSize}");
                }
            }
        }

#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        private void ReadRawTile(Stream stream, bool hasTargetFramebuffer, ref FramebufferCursor framebufferCursor, in Rectangle tile, in PixelFormat cPixelFormat)
        {
            // Calculate how many bytes we're going to receive
            byte bytesPerPixel = cPixelFormat.BytesPerPixel;
            int totalBytesToRead = tile.Size.Width * tile.Size.Height * bytesPerPixel;

            // If there is nothing to render to, just skip the received bytes
            if (!hasTargetFramebuffer)
            {
                stream.SkipAll(totalBytesToRead);
                return;
            }

            // Read raw data
            Span<byte> buffer = _tileBuffer.AsSpan().Slice(0, totalBytesToRead);
            stream.ReadAll(buffer);

            // Process all bytes and draw to the framebuffer
            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                {
                    for (var processedBytes = 0; processedBytes < totalBytesToRead; processedBytes += bytesPerPixel)
                    {
                        // Set the pixel
                        framebufferCursor.SetPixel(bufferPtr + processedBytes, cPixelFormat);
                        if(!framebufferCursor.GetEndReached())
                            framebufferCursor.MoveNext();
                    }
                }
            }
        }

#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        private void ReadSolidTile(Stream stream, bool hasTargetFramebuffer, ref FramebufferCursor framebufferCursor, in Rectangle tile, in PixelFormat cPixelFormat)
        {
            // Read a single color value
            Span<byte> buffer = _tileBuffer.AsSpan().Slice(0, cPixelFormat.BytesPerPixel);
            stream.ReadAll(buffer);

            // Skip rendering if there is nothing to render to.
            if (!hasTargetFramebuffer)
                return;

            // Fill the tile with a solid color
            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                    framebufferCursor.SetPixelsSolid(bufferPtr, cPixelFormat, tile.Size.Width * tile.Size.Height);
            }
        }

#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        private void ReadPackedPaletteTile(Stream stream, bool hasTargetFramebuffer, ref FramebufferCursor framebufferCursor, in Rectangle tile, in PixelFormat cPixelFormat,
            int paletteSize)
        {
            // Calculate how many bytes we're going to receive
            byte bytesPerPixel = cPixelFormat.BytesPerPixel;
            int paletteBytes = paletteSize * bytesPerPixel;
            int bitsPerPackedPixel = paletteSize > 4 ? 4 : paletteSize > 2 ? 2 : 1;
            int packedPixelBytes = bitsPerPackedPixel switch {
                1 => (tile.Size.Width + 7) / 8 * tile.Size.Height,
                2 => (tile.Size.Width + 3) / 4 * tile.Size.Height,
                4 => (tile.Size.Width + 1) / 2 * tile.Size.Height
            };
            int totalBytesToRead = paletteBytes + packedPixelBytes;

            // If there is nothing to render to, just skip the received bytes
            if (!hasTargetFramebuffer)
            {
                stream.SkipAll(totalBytesToRead);
                return;
            }

            // Read all data. Buffer is always large enough: 16 * 4 + (64 + 1) / 2 * 64 = 2144 < 16384 = 64 * 64 * 4
            Span<byte> buffer = _tileBuffer.AsSpan().Slice(0, totalBytesToRead);
            stream.ReadAll(buffer);

            Span<byte> palette = buffer.Slice(0, paletteBytes);
            Span<byte> packedPixels = buffer.Slice(paletteBytes, packedPixelBytes);

            var indexMask = (byte)(((1 << bitsPerPackedPixel) - 1) & 127);

            // https://github.com/TigerVNC/tigervnc/blob/a356a706526ac4182b3ae144166ae04271b85258/java/com/tigervnc/rfb/ZRLEDecoder.java#L213
            unsafe
            {
                fixed (byte* palettePtr = palette)
                fixed (byte* pixelsPtr = packedPixels)
                {
                    byte* nextPixelsBytePtr = pixelsPtr;

                    // Process the pixels line by line
                    for (var i = 0; i < tile.Size.Height; i++)
                    {
                        byte pixelsByte = 0;
                        var remainingBits = 0;

                        // Write pixels left to right
                        for (var j = 0; j < tile.Size.Width; j++)
                        {
                            // Read next byte?
                            if (remainingBits == 0)
                            {
                                pixelsByte = *nextPixelsBytePtr++;
                                remainingBits = 8;
                            }

                            // Get palette index
                            remainingBits -= bitsPerPackedPixel;
                            int paletteIndex = ((pixelsByte >> remainingBits) & indexMask) * bytesPerPixel;
                            if (paletteIndex >= paletteBytes)
                                throw new UnexpectedDataException($"Received invalid packed palette index of {paletteIndex} for a palette of {paletteBytes} bytes.");

                            // Set the pixel
                            framebufferCursor.SetPixel(palettePtr + paletteIndex, cPixelFormat);
                            if(!framebufferCursor.GetEndReached())
                                framebufferCursor.MoveNext();
                        }
                    }
                }
            }
        }

#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        private void ReadRleTile(Stream stream, bool hasTargetFramebuffer, ref FramebufferCursor framebufferCursor, in Rectangle tile, in PixelFormat cPixelFormat)
        {
            Span<byte> buffer = _tileBuffer.AsSpan();
            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                {
                    // We need to read on demand, because we cannot tell the number of bytes to read in advance
                    Span<byte> runLengthReadBuffer = stackalloc byte[1];

                    int pixelsRemaining = tile.Size.Width * tile.Size.Height;
                    while (pixelsRemaining > 0)
                    {
                        // Read pixel data and first run-length byte
                        stream.ReadAll(buffer.Slice(0, 4));

                        // Calculate run-length
                        byte runLengthByte = buffer[3];
                        int runLength = runLengthByte + 1;
                        while (runLengthByte == 255)
                        {
                            // Read next run-length byte
                            if (stream.Read(runLengthReadBuffer) == 0)
                                throw new UnexpectedEndOfStreamException("Stream reached its end while reading ZRLE RLE run-length.");
                            runLengthByte = runLengthReadBuffer[0];
                            runLength += runLengthByte;
                        }

                        // Is there anything to render to?
                        if (hasTargetFramebuffer)
                        {
                            // Set as much pixels as run-length
                            framebufferCursor.SetPixelsSolid(bufferPtr, cPixelFormat, runLength);
                        }

                        pixelsRemaining -= runLength;
                    }
                }
            }
        }

#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        private void ReadPaletteRleTile(Stream stream, bool hasTargetFramebuffer, ref FramebufferCursor framebufferCursor, in Rectangle tile, in PixelFormat cPixelFormat,
            int paletteSize)
        {
            byte bytesPerPixel = cPixelFormat.BytesPerPixel;
            int paletteBytes = paletteSize * bytesPerPixel;

            // Read palette bytes
            Span<byte> palette = _tileBuffer.AsSpan().Slice(0, paletteBytes);
            stream.ReadAll(palette);

            unsafe
            {
                fixed (byte* palettePtr = palette)
                {
                    // We need to read on demand, because we cannot tell the number of bytes to read in advance
                    Span<byte> readBuffer = stackalloc byte[1];

                    int pixelsRemaining = tile.Size.Width * tile.Size.Height;
                    while (pixelsRemaining > 0)
                    {
                        // Read palette index
                        if (stream.Read(readBuffer) == 0)
                            throw new UnexpectedEndOfStreamException("Stream reached its end while reading ZRLE RLE palette index.");
                        byte paletteIndexByte = readBuffer[0];

                        // Is this a longer run?
                        var runLength = 1;
                        if ((paletteIndexByte & 128) != 0)
                        {
                            byte runLengthByte;
                            do
                            {
                                // Read next run-length byte
                                if (stream.Read(readBuffer) == 0)
                                    throw new UnexpectedEndOfStreamException("Stream reached its end while reading ZRLE RLE run-length.");
                                runLengthByte = readBuffer[0];
                                runLength += runLengthByte;
                            }
                            while (runLengthByte == 255);
                        }

                        // Is there anything to render to?
                        if (hasTargetFramebuffer)
                        {
                            // Calculate palette index
                            int paletteIndex = (paletteIndexByte & 127) * bytesPerPixel;
                            if (paletteIndex >= paletteBytes)
                                throw new UnexpectedDataException($"Received invalid RLE palette index of {paletteIndex} for a palette of {paletteBytes} bytes.");

                            // Set as much pixels as run-length
                            framebufferCursor.SetPixelsSolid(palettePtr + paletteIndex, cPixelFormat, runLength);
                        }

                        pixelsRemaining -= runLength;
                    }
                }
            }
        }
    }
}
