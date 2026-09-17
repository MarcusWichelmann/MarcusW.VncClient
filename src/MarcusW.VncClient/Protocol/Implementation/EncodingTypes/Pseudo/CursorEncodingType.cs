using System;
using System.Buffers;
using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Rendering;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding type for receiving cursor shape updates from the server.
    /// </summary>
    /// <remarks>
    /// The cursor pseudo-encoding allows the server to send the cursor shape to the client,
    /// enabling local cursor rendering which significantly improves perceived performance
    /// over slow network connections.
    /// </remarks>
    public class CursorEncodingType : PseudoEncodingType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<CursorEncodingType> _logger;
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.Cursor;

        /// <inheritdoc />
        public override string Name => "Cursor";

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="CursorEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public CursorEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<CursorEncodingType>();
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
        {
            // The x-position and y-position indicate the hotspot
            int hotspotX = rectangle.Position.X;
            int hotspotY = rectangle.Position.Y;
            int width = rectangle.Size.Width;
            int height = rectangle.Size.Height;

            // Handle empty cursor (hide cursor)
            if (width == 0 || height == 0)
            {
                _logger.LogDebug("Received empty cursor (hide cursor)");
                _context.Connection.CursorHandler?.HideCursor();
                return;
            }

            // Sanity check cursor size
            if (width > 256 || height > 256)
            {
                _logger.LogWarning("Cursor size too large ({Width}x{Height}), skipping", width, height);
                SkipCursorData(transportStream, width, height);
                return;
            }

            PixelFormat format = _state.RemoteFramebufferFormat;
            int bytesPerPixel = format.BytesPerPixel;
            int pixelDataSize = width * height * bytesPerPixel;
            int bitmaskSize = ((width + 7) / 8) * height;

            // Rent buffers
            byte[] pixelData = ArrayPool<byte>.Shared.Rent(pixelDataSize);
            byte[] bitmask = ArrayPool<byte>.Shared.Rent(bitmaskSize);

            try
            {
                // Read pixel data
                transportStream.ReadAll(pixelData.AsSpan(0, pixelDataSize));

                // Read bitmask
                transportStream.ReadAll(bitmask.AsSpan(0, bitmaskSize));

                _logger.LogDebug("Received cursor: {Width}x{Height}, hotspot ({HotspotX}, {HotspotY})", 
                    width, height, hotspotX, hotspotY);

                // Notify the cursor handler
                ICursorHandler? cursorHandler = _context.Connection.CursorHandler;
                if (cursorHandler != null)
                {
                    // Copy to exact-sized arrays for the handler
                    byte[] exactPixelData = new byte[pixelDataSize];
                    byte[] exactBitmask = new byte[bitmaskSize];
                    Array.Copy(pixelData, exactPixelData, pixelDataSize);
                    Array.Copy(bitmask, exactBitmask, bitmaskSize);

                    cursorHandler.UpdateCursor(hotspotX, hotspotY, width, height, exactPixelData, exactBitmask, format);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pixelData);
                ArrayPool<byte>.Shared.Return(bitmask);
            }
        }

        private void SkipCursorData(Stream transportStream, int width, int height)
        {
            PixelFormat format = _state.RemoteFramebufferFormat;
            int bytesPerPixel = format.BytesPerPixel;
            int pixelDataSize = width * height * bytesPerPixel;
            int bitmaskSize = ((width + 7) / 8) * height;
            transportStream.SkipAll(pixelDataSize + bitmaskSize);
        }
    }
}
