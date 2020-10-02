using System;
using System.Buffers.Binary;
using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Frame
{
    /// <summary>
    /// A frame encoding for copying a rectangle to another place.
    /// </summary>
    public class CopyRectEncodingType : FrameEncodingType
    {
        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.CopyRect;

        /// <inheritdoc />
        public override string Name => "CopyRect";

        /// <inheritdoc />
        public override int Priority => 1000;

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <inheritdoc />
        public override Color VisualizationColor => new Color(255, 0, 255);

        /// <inheritdoc />
        public override void ReadFrameEncoding(Stream transportStream, IFramebufferReference? targetFramebuffer, in Rectangle rectangle, in Size remoteFramebufferSize,
            in PixelFormat remoteFramebufferFormat)
        {
            if (transportStream == null)
                throw new ArgumentNullException(nameof(transportStream));

            // Read header
            Span<byte> header = stackalloc byte[4];
            transportStream.ReadAll(header);
            uint srcX = BinaryPrimitives.ReadUInt16BigEndian(header);
            uint srcY = BinaryPrimitives.ReadUInt16BigEndian(header[2..]);

            // Anything to do?
            if (targetFramebuffer == null)
                return;

            // Calculate source rectangle with same size at a different position
            Rectangle sourceRectangle = rectangle.WithPosition(new Position((int)srcX, (int)srcY));

            // Create cursors for finding the source and target positions in the framebuffer
            var sourceCursor = new FramebufferCursor(targetFramebuffer, sourceRectangle);
            var targetCursor = new FramebufferCursor(targetFramebuffer, rectangle);

            // Copy pixels
            targetCursor.CopyAllFrom(ref sourceCursor);
        }
    }
}
