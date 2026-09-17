using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Frame
{
    /// <summary>
    /// A frame encoding type for CoRRE (Compressed Rise-and-Run-length Encoding) pixel data.
    /// </summary>
    /// <remarks>
    /// CoRRE is a variant of RRE where position and size of subrectangles are limited to 255 pixels,
    /// allowing them to be stored in single bytes instead of 16-bit values.
    /// </remarks>
    public class CorreEncodingType : FrameEncodingType
    {
        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.CoRRE;

        /// <inheritdoc />
        public override string Name => "CoRRE";

        /// <inheritdoc />
        public override int Priority => 40;

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <inheritdoc />
        public override Color VisualizationColor => new Color(255, 64, 128);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void ReadFrameEncoding(Stream transportStream, IFramebufferReference? targetFramebuffer, in Rectangle rectangle, in Size remoteFramebufferSize,
            in PixelFormat remoteFramebufferFormat)
        {
            if (transportStream == null)
                throw new ArgumentNullException(nameof(transportStream));

            int bytesPerPixel = remoteFramebufferFormat.BytesPerPixel;

            // Read header: number of subrectangles (4 bytes) + background pixel
            Span<byte> header = stackalloc byte[4 + bytesPerPixel];
            transportStream.ReadAll(header);

            uint numSubrects = BinaryPrimitives.ReadUInt32BigEndian(header);
            Span<byte> backgroundPixel = header.Slice(4, bytesPerPixel);

            // Fill the entire rectangle with the background color
            if (targetFramebuffer != null)
            {
                var cursor = new FramebufferCursor(targetFramebuffer, rectangle);
                unsafe
                {
                    fixed (byte* bgPtr = backgroundPixel)
                    {
                        cursor.SetPixelsSolid(bgPtr, remoteFramebufferFormat, rectangle.Size.Width * rectangle.Size.Height);
                    }
                }
            }

            // Process subrectangles
            // Each subrect in CoRRE: pixel value + x (U8) + y (U8) + width (U8) + height (U8)
            int subrectSize = bytesPerPixel + 4;
            byte[] subrectBuffer = ArrayPool<byte>.Shared.Rent(subrectSize);
            Span<byte> subrect = subrectBuffer.AsSpan(0, subrectSize);

            try
            {
                for (uint i = 0; i < numSubrects; i++)
                {
                    transportStream.ReadAll(subrect);

                    Span<byte> subrectPixel = subrect.Slice(0, bytesPerPixel);
                    byte subX = subrect[bytesPerPixel];
                    byte subY = subrect[bytesPerPixel + 1];
                    byte subW = subrect[bytesPerPixel + 2];
                    byte subH = subrect[bytesPerPixel + 3];

                    if (targetFramebuffer != null && subW > 0 && subH > 0)
                    {
                        var subRect = new Rectangle(
                            new Position(rectangle.Position.X + subX, rectangle.Position.Y + subY),
                            new Size(subW, subH));

                        var subCursor = new FramebufferCursor(targetFramebuffer, subRect);
                        unsafe
                        {
                            fixed (byte* pixelPtr = subrectPixel)
                            {
                                subCursor.SetPixelsSolid(pixelPtr, remoteFramebufferFormat, subW * subH);
                            }
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(subrectBuffer);
            }
        }
    }
}
