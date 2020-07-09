using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.Services;
using MarcusW.VncClient.Utils;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Initialization
{
    /// <inhertitdoc />
    public class RfbInitializer : IRfbInitializer
    {
        private readonly RfbConnectionContext _context;
        private readonly ProtocolState _state;
        private readonly ILogger<RfbInitializer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RfbInitializer"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public RfbInitializer(RfbConnectionContext context)
        {
            _context = context;
            _state = context.GetState<ProtocolState>();
            _logger = context.Connection.LoggerFactory.CreateLogger<RfbInitializer>();
        }

        /// <inheritdoc />
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Debug.Assert(_context.Transport != null, "_context.Transport != null");

            _logger.LogDebug("Doing protocol initialization...");

            ITransport transport = _context.Transport;

            // Send ClientInit message
            await SendClientInitAsync(transport, cancellationToken).ConfigureAwait(false);

            // Read ServerInit response
            (FrameSize framebufferSize, PixelFormat pixelFormat, string desktopName) = await ReadServerInitAsync(transport, cancellationToken).ConfigureAwait(false);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Framebuffer size: {framebufferSize}", framebufferSize);
                _logger.LogDebug("Pixel format: {pixelFormat}", pixelFormat);
                _logger.LogDebug("Desktop name: {desktopName}", desktopName);
            }

            // Update state
            _state.FramebufferSize = framebufferSize;
            _state.FramebufferFormat = pixelFormat;
            _state.DesktopName = desktopName;

            // Some security types extend the ServerInit response and now have the chance to continue reading
            Debug.Assert(_state.UsedSecurityType != null, "_state.UsedSecurityType != null");
            await _state.UsedSecurityType.ReadServerInitExtensionAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task SendClientInitAsync(ITransport transport, CancellationToken cancellationToken = default)
        {
            bool shared = _context.Connection.Parameters.AllowSharedConnection;
            _logger.LogDebug("Sending shared-flag ({shared})...", shared);

            await transport.Stream.WriteAsync(new[] { (byte)(shared ? 1 : 0) }, cancellationToken).ConfigureAwait(false);
        }

        private async Task<(FrameSize framebufferSize, PixelFormat pixelFormat, string desktopName)> ReadServerInitAsync(ITransport transport,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Reading server init message...");

            // Read the first part of the message of which the length is known
            ReadOnlyMemory<byte> headerBytes = await transport.Stream.ReadAllBytesAsync(24, cancellationToken).ConfigureAwait(false);
            FrameSize framebufferSize = GetFramebufferSize(headerBytes.Span[..4]);
            PixelFormat pixelFormat = GetPixelFormat(headerBytes.Span[4..20]);
            uint desktopNameLength = BinaryPrimitives.ReadUInt32BigEndian(headerBytes.Span[20..24]);

            // Read desktop name
            ReadOnlyMemory<byte> desktopNameBytes = await transport.Stream.ReadAllBytesAsync((int)desktopNameLength, cancellationToken).ConfigureAwait(false);
            string desktopName = Encoding.UTF8.GetString(desktopNameBytes.Span);

            return (framebufferSize, pixelFormat, desktopName);
        }

        private static FrameSize GetFramebufferSize(ReadOnlySpan<byte> headerBytes)
        {
            ushort framebufferWidth = BinaryPrimitives.ReadUInt16BigEndian(headerBytes[..2]);
            ushort framebufferHeight = BinaryPrimitives.ReadUInt16BigEndian(headerBytes[2..4]);

            return new FrameSize(framebufferWidth, framebufferHeight);
        }

        private static PixelFormat GetPixelFormat(ReadOnlySpan<byte> headerBytes)
        {
            byte bitsPerPixel = headerBytes[0];
            byte depth = headerBytes[1];
            bool bigEndian = headerBytes[2] != 0;
            bool trueColor = headerBytes[3] != 0;
            ushort redMax = BinaryPrimitives.ReadUInt16BigEndian(headerBytes[4..6]);
            ushort greenMax = BinaryPrimitives.ReadUInt16BigEndian(headerBytes[6..8]);
            ushort blueMax = BinaryPrimitives.ReadUInt16BigEndian(headerBytes[8..10]);
            byte redShift = headerBytes[10];
            byte greenShift = headerBytes[11];
            byte blueShift = headerBytes[12];

            // Remaining 3 bytes are padding

            // TODO: Add support for color maps
            if (!trueColor)
                throw new UnsupportedProtocolFeatureException("Color maps are currently not supported by this client.");

            return new PixelFormat(bitsPerPixel, depth, bigEndian, trueColor, redMax, greenMax, blueMax, redShift, greenShift, blueShift);
        }
    }
}