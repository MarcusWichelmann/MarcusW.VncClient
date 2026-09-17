using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Frame
{
    /// <summary>
    /// Hextile subencoding flags.
    /// </summary>
    [Flags]
    internal enum HextileSubencoding : byte
    {
        /// <summary>
        /// Tile contains raw pixel data.
        /// </summary>
        Raw = 1,

        /// <summary>
        /// Background pixel value follows.
        /// </summary>
        BackgroundSpecified = 2,

        /// <summary>
        /// Foreground pixel value follows.
        /// </summary>
        ForegroundSpecified = 4,

        /// <summary>
        /// Subrectangle data follows.
        /// </summary>
        AnySubrects = 8,

        /// <summary>
        /// Subrectangles are colored (each has its own pixel value).
        /// </summary>
        SubrectsColored = 16
    }

    /// <summary>
    /// A frame encoding type for hextile-encoded pixel data.
    /// </summary>
    /// <remarks>
    /// Hextile is a variation on RRE encoding where rectangles are split into 16x16 tiles.
    /// This allows for more efficient encoding of regions with varying complexity.
    /// </remarks>
    public class HextileEncodingType : FrameEncodingType
    {
        private const int TileSize = 16;
        private const int MaxTilePixels = TileSize * TileSize;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.Hextile;

        /// <inheritdoc />
        public override string Name => "Hextile";

        /// <inheritdoc />
        public override int Priority => 50;

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <inheritdoc />
        public override Color VisualizationColor => new Color(128, 255, 0);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void ReadFrameEncoding(Stream transportStream, IFramebufferReference? targetFramebuffer, in Rectangle rectangle, in Size remoteFramebufferSize,
            in PixelFormat remoteFramebufferFormat)
        {
            if (transportStream == null)
                throw new ArgumentNullException(nameof(transportStream));

            int bytesPerPixel = remoteFramebufferFormat.BytesPerPixel;
            int rectWidth = rectangle.Size.Width;
            int rectHeight = rectangle.Size.Height;
            int rectX = rectangle.Position.X;
            int rectY = rectangle.Position.Y;

            // Background and foreground pixels that carry over between tiles
            byte[] backgroundPixel = new byte[bytesPerPixel];
            byte[] foregroundPixel = new byte[bytesPerPixel];
            bool backgroundValid = false;

            // Buffer for raw tile data
            byte[] rawBuffer = ArrayPool<byte>.Shared.Rent(MaxTilePixels * bytesPerPixel);

            try
            {
                // Process tiles in left-to-right, top-to-bottom order
                for (int tileY = 0; tileY < rectHeight; tileY += TileSize)
                {
                    for (int tileX = 0; tileX < rectWidth; tileX += TileSize)
                    {
                        // Calculate actual tile dimensions (may be smaller at edges)
                        int tileWidth = Math.Min(TileSize, rectWidth - tileX);
                        int tileHeight = Math.Min(TileSize, rectHeight - tileY);

                        // Read subencoding byte
                        int subencodingByte = transportStream.ReadByte();
                        if (subencodingByte == -1)
                            throw new UnexpectedEndOfStreamException("Stream ended while reading Hextile subencoding.");

                        var subencoding = (HextileSubencoding)subencodingByte;

                        // Create tile rectangle
                        var tileRect = new Rectangle(
                            new Position(rectX + tileX, rectY + tileY),
                            new Size(tileWidth, tileHeight));

                        if (subencoding.HasFlag(HextileSubencoding.Raw))
                        {
                            // Raw tile data
                            int rawSize = tileWidth * tileHeight * bytesPerPixel;
                            transportStream.ReadAll(rawBuffer.AsSpan(0, rawSize));

                            if (targetFramebuffer != null)
                            {
                                RenderRawTile(targetFramebuffer, tileRect, rawBuffer, bytesPerPixel, remoteFramebufferFormat);
                            }

                            // Raw tile doesn't carry over background/foreground
                            backgroundValid = false;
                            continue;
                        }

                        // BackgroundSpecified
                        if (subencoding.HasFlag(HextileSubencoding.BackgroundSpecified))
                        {
                            transportStream.ReadAll(backgroundPixel.AsSpan(0, bytesPerPixel));
                            backgroundValid = true;
                        }

                        if (!backgroundValid)
                            throw new InvalidDataException("Hextile tile requires background but none was specified.");

                        // ForegroundSpecified
                        if (subencoding.HasFlag(HextileSubencoding.ForegroundSpecified))
                        {
                            transportStream.ReadAll(foregroundPixel.AsSpan(0, bytesPerPixel));
                        }

                        // Fill tile with background color
                        if (targetFramebuffer != null)
                        {
                            FillTile(targetFramebuffer, tileRect, backgroundPixel, bytesPerPixel, remoteFramebufferFormat);
                        }

                        // AnySubrects
                        if (subencoding.HasFlag(HextileSubencoding.AnySubrects))
                        {
                            int numSubrects = transportStream.ReadByte();
                            if (numSubrects == -1)
                                throw new UnexpectedEndOfStreamException("Stream ended while reading Hextile subrect count.");

                            bool subrectsColored = subencoding.HasFlag(HextileSubencoding.SubrectsColored);
                            byte[] subrectPixel = subrectsColored ? new byte[bytesPerPixel] : foregroundPixel;

                            for (int i = 0; i < numSubrects; i++)
                            {
                                // Read subrect color if SubrectsColored
                                if (subrectsColored)
                                {
                                    transportStream.ReadAll(subrectPixel.AsSpan(0, bytesPerPixel));
                                }

                                // Read position and size
                                int xyByte = transportStream.ReadByte();
                                int whByte = transportStream.ReadByte();
                                if (xyByte == -1 || whByte == -1)
                                    throw new UnexpectedEndOfStreamException("Stream ended while reading Hextile subrect.");

                                int subX = (xyByte >> 4) & 0x0F;
                                int subY = xyByte & 0x0F;
                                int subW = ((whByte >> 4) & 0x0F) + 1;
                                int subH = (whByte & 0x0F) + 1;

                                // Draw subrectangle
                                if (targetFramebuffer != null)
                                {
                                    var subRect = new Rectangle(
                                        new Position(rectX + tileX + subX, rectY + tileY + subY),
                                        new Size(subW, subH));
                                    FillTile(targetFramebuffer, subRect, subrectPixel, bytesPerPixel, remoteFramebufferFormat);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawBuffer);
            }
        }

        private static void RenderRawTile(IFramebufferReference framebuffer, Rectangle tileRect, byte[] data, int bytesPerPixel, PixelFormat format)
        {
            var cursor = new FramebufferCursor(framebuffer, tileRect);
            int offset = 0;

            unsafe
            {
                fixed (byte* dataPtr = data)
                {
                    int pixelCount = tileRect.Size.Width * tileRect.Size.Height;
                    for (int i = 0; i < pixelCount; i++)
                    {
                        cursor.SetPixel(dataPtr + offset, format);
                        if (!cursor.GetEndReached())
                            cursor.MoveNext();
                        offset += bytesPerPixel;
                    }
                }
            }
        }

        private static void FillTile(IFramebufferReference framebuffer, Rectangle tileRect, byte[] pixel, int bytesPerPixel, PixelFormat format)
        {
            var cursor = new FramebufferCursor(framebuffer, tileRect);

            unsafe
            {
                fixed (byte* pixelPtr = pixel)
                {
                    int pixelCount = tileRect.Size.Width * tileRect.Size.Height;
                    for (int i = 0; i < pixelCount; i++)
                    {
                        cursor.SetPixel(pixelPtr, format);
                        if (!cursor.GetEndReached())
                            cursor.MoveNext();
                    }
                }
            }
        }
    }
}
