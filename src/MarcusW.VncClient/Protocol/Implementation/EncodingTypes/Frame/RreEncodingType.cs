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
    /// A frame encoding type for RRE (Rise-and-Run-length Encoding) compressed pixel data.
    /// </summary>
    /// <remarks>
    /// RRE is essentially a two-dimensional analogue of run-length encoding.
    /// Rectangles are partitioned into subrectangles of a single color.
    /// </remarks>
    public class RreEncodingType : FrameEncodingType
    {
        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.RRE;

        /// <inheritdoc />
        public override string Name => "RRE";

        /// <inheritdoc />
        public override int Priority => 30;

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <inheritdoc />
        public override Color VisualizationColor => new Color(255, 128, 0);

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
            // Each subrect: pixel value + x (U16) + y (U16) + width (U16) + height (U16)
            int subrectSize = bytesPerPixel + 8;
            byte[] subrectBuffer = ArrayPool<byte>.Shared.Rent(subrectSize);
            Span<byte> subrect = subrectBuffer.AsSpan(0, subrectSize);

            try
            {
                for (uint i = 0; i < numSubrects; i++)
                {
                    transportStream.ReadAll(subrect);

                    Span<byte> subrectPixel = subrect.Slice(0, bytesPerPixel);
                    ushort subX = BinaryPrimitives.ReadUInt16BigEndian(subrect.Slice(bytesPerPixel));
                    ushort subY = BinaryPrimitives.ReadUInt16BigEndian(subrect.Slice(bytesPerPixel + 2));
                    ushort subW = BinaryPrimitives.ReadUInt16BigEndian(subrect.Slice(bytesPerPixel + 4));
                    ushort subH = BinaryPrimitives.ReadUInt16BigEndian(subrect.Slice(bytesPerPixel + 6));

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
