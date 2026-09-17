using System;
using System.Buffers;
using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Rendering;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding type for receiving X cursor shape updates from the server.
    /// </summary>
    /// <remarks>
    /// The X cursor pseudo-encoding is a simpler two-color cursor format that uses
    /// primary and secondary colors with a bitmap and bitmask.
    /// </remarks>
    public class XCursorEncodingType : PseudoEncodingType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<XCursorEncodingType> _logger;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.XCursor;

        /// <inheritdoc />
        public override string Name => "XCursor";

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="XCursorEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public XCursorEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<XCursorEncodingType>();
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
                _logger.LogDebug("Received empty X cursor (hide cursor)");
                _context.Connection.CursorHandler?.HideCursor();
                return;
            }

            // Sanity check cursor size
            if (width > 256 || height > 256)
            {
                _logger.LogWarning("X cursor size too large ({Width}x{Height}), skipping", width, height);
                SkipXCursorData(transportStream, width, height);
                return;
            }

            // X Cursor format:
            // 3 bytes - primary RGB
            // 3 bytes - secondary RGB
            // floor((width + 7) / 8) * height bytes - bitmap (1 = primary, 0 = secondary)
            // floor((width + 7) / 8) * height bytes - bitmask (1 = visible)

            int scanlineSize = (width + 7) / 8;
            int bitmapSize = scanlineSize * height;

            // Read primary and secondary colors
            Span<byte> primaryColor = stackalloc byte[3];
            Span<byte> secondaryColor = stackalloc byte[3];
            transportStream.ReadAll(primaryColor);
            transportStream.ReadAll(secondaryColor);

            // Rent buffers
            byte[] bitmap = ArrayPool<byte>.Shared.Rent(bitmapSize);
            byte[] bitmask = ArrayPool<byte>.Shared.Rent(bitmapSize);

            try
            {
                // Read bitmap
                transportStream.ReadAll(bitmap.AsSpan(0, bitmapSize));

                // Read bitmask
                transportStream.ReadAll(bitmask.AsSpan(0, bitmapSize));

                _logger.LogDebug("Received X cursor: {Width}x{Height}, hotspot ({HotspotX}, {HotspotY})", 
                    width, height, hotspotX, hotspotY);

                // Notify the cursor handler
                ICursorHandler? cursorHandler = _context.Connection.CursorHandler;
                if (cursorHandler != null)
                {
                    // Copy to exact-sized arrays for the handler
                    byte[] exactPrimaryColor = primaryColor.ToArray();
                    byte[] exactSecondaryColor = secondaryColor.ToArray();
                    byte[] exactBitmap = new byte[bitmapSize];
                    byte[] exactBitmask = new byte[bitmapSize];
                    Array.Copy(bitmap, exactBitmap, bitmapSize);
                    Array.Copy(bitmask, exactBitmask, bitmapSize);

                    cursorHandler.UpdateXCursor(hotspotX, hotspotY, width, height, 
                        exactPrimaryColor, exactSecondaryColor, exactBitmap, exactBitmask);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bitmap);
                ArrayPool<byte>.Shared.Return(bitmask);
            }
        }

        private static void SkipXCursorData(Stream transportStream, int width, int height)
        {
            int scanlineSize = (width + 7) / 8;
            int bitmapSize = scanlineSize * height;
            // Skip: 3 (primary) + 3 (secondary) + bitmap + bitmask
            transportStream.SkipAll(6 + bitmapSize * 2);
        }
    }
}
