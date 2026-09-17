using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Rendering;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding type for receiving cursor shape updates with alpha channel from the server.
    /// </summary>
    /// <remarks>
    /// The cursor with alpha pseudo-encoding provides modern RGBA cursor support with
    /// premultiplied alpha transparency, allowing for smooth, anti-aliased cursors.
    /// </remarks>
    public class CursorWithAlphaEncodingType : PseudoEncodingType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<CursorWithAlphaEncodingType> _logger;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.CursorWithAlpha;

        /// <inheritdoc />
        public override string Name => "CursorWithAlpha";

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="CursorWithAlphaEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public CursorWithAlphaEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<CursorWithAlphaEncodingType>();
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
                _logger.LogDebug("Received empty cursor with alpha (hide cursor)");
                _context.Connection.CursorHandler?.HideCursor();
                return;
            }

            // Sanity check cursor size
            if (width > 256 || height > 256)
            {
                _logger.LogWarning("Cursor with alpha size too large ({Width}x{Height}), skipping", width, height);
                SkipCursorData(transportStream, width, height);
                return;
            }

            // Cursor with alpha format:
            // 4 bytes - encoding type (S32)
            // width * height * 4 bytes - RGBA pixel data (premultiplied alpha)

            // Read the encoding type
            Span<byte> encodingBuffer = stackalloc byte[4];
            transportStream.ReadAll(encodingBuffer);
            int encodingType = BinaryPrimitives.ReadInt32BigEndian(encodingBuffer);

            // The encoding type should be Raw (0) for most implementations
            // but the spec says it can be any encoding the client has declared
            if (encodingType != (int)WellKnownEncodingType.Raw)
            {
                _logger.LogDebug("Cursor with alpha uses encoding type {EncodingType}", encodingType);
                // For now, we only support raw encoding
                if (encodingType != 0)
                {
                    _logger.LogWarning("Unsupported cursor encoding type {EncodingType}, treating as raw", encodingType);
                }
            }

            int rgbaDataSize = width * height * 4;

            // Rent buffer
            byte[] rgbaData = ArrayPool<byte>.Shared.Rent(rgbaDataSize);

            try
            {
                // Read RGBA pixel data
                transportStream.ReadAll(rgbaData.AsSpan(0, rgbaDataSize));

                _logger.LogDebug("Received cursor with alpha: {Width}x{Height}, hotspot ({HotspotX}, {HotspotY})", 
                    width, height, hotspotX, hotspotY);

                // Notify the cursor handler
                ICursorHandler? cursorHandler = _context.Connection.CursorHandler;
                if (cursorHandler != null)
                {
                    // Copy to exact-sized array for the handler
                    byte[] exactRgbaData = new byte[rgbaDataSize];
                    Array.Copy(rgbaData, exactRgbaData, rgbaDataSize);

                    cursorHandler.UpdateCursorWithAlpha(hotspotX, hotspotY, width, height, exactRgbaData);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rgbaData);
            }
        }

        private static void SkipCursorData(Stream transportStream, int width, int height)
        {
            // Skip: 4 (encoding) + width * height * 4 (RGBA data)
            int rgbaDataSize = width * height * 4;
            transportStream.SkipAll(4 + rgbaDataSize);
        }
    }
}
