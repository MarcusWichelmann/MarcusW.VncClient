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
            (Size framebufferSize, PixelFormat pixelFormat, string desktopName) = await ReadServerInitAsync(transport, cancellationToken).ConfigureAwait(false);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Framebuffer size: {framebufferSize}", framebufferSize);
                _logger.LogDebug("Pixel format: {pixelFormat}", pixelFormat);
                _logger.LogDebug("Desktop name: {desktopName}", desktopName);
            }

            // Update state
            _state.RemoteFramebufferSize = framebufferSize;
            _state.RemoteFramebufferFormat = pixelFormat;
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

        private async Task<(Size framebufferSize, PixelFormat pixelFormat, string desktopName)> ReadServerInitAsync(ITransport transport,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Reading server init message...");

            // Read the first part of the message of which the length is known
            ReadOnlyMemory<byte> headerBytes = await transport.Stream.ReadAllAsync(24, cancellationToken).ConfigureAwait(false);
            Size framebufferSize = GetFramebufferSize(headerBytes.Span[..4]);
            PixelFormat pixelFormat = GetPixelFormat(headerBytes.Span[4..20]);
            uint desktopNameLength = BinaryPrimitives.ReadUInt32BigEndian(headerBytes.Span[20..24]);

            // Read desktop name
            ReadOnlyMemory<byte> desktopNameBytes = await transport.Stream.ReadAllAsync((int)desktopNameLength, cancellationToken).ConfigureAwait(false);
            string desktopName = Encoding.UTF8.GetString(desktopNameBytes.Span);

            return (framebufferSize, pixelFormat, desktopName);
        }

        private static Size GetFramebufferSize(ReadOnlySpan<byte> headerBytes)
        {
            ushort framebufferWidth = BinaryPrimitives.ReadUInt16BigEndian(headerBytes[..2]);
            ushort framebufferHeight = BinaryPrimitives.ReadUInt16BigEndian(headerBytes[2..4]);

            return new Size(framebufferWidth, framebufferHeight);
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

            // Check bits per pixel
            if (bitsPerPixel > 32)
                throw new UnexpectedDataException("The bits per pixel value of the received pixel format is too high. The maximum is 32bpp.");

            // Get the used bits per channel
            byte redBits = PixelUtils.GetChannelDepth(redMax);
            byte greenBits = PixelUtils.GetChannelDepth(greenMax);
            byte blueBits = PixelUtils.GetChannelDepth(blueMax);

            // Check the depth value
            if (redBits + greenBits + blueBits > depth)
                throw new UnexpectedDataException("The received pixel format is invalid. The depth value must not be smaller than the sum of the used bits per channel.");

            // Check the shift values
            if (redBits + redShift > bitsPerPixel || greenBits + greenShift > bitsPerPixel || blueBits + blueShift > bitsPerPixel)
                throw new UnexpectedDataException("The color shift values in the received pixel format are invalid.");

            // Check for overlaps
            uint redMask = (uint)redMax << redShift;
            uint greenMask = (uint)greenMax << greenShift;
            uint blueMask = (uint)blueMax << blueShift;
            if (((redMask & greenMask) | (greenMask & blueMask) | (blueMask & redMask)) != 0)
                throw new UnexpectedDataException("The bits of the color channels in the received pixel format must not overlap.");

            // Generate a short name for this pixel format while following the RFB naming scheme (name describes the native byte order, e.g. 0xRGB).
            string name;
            if (redShift > greenShift && greenShift > blueShift)
                name = $"RGB{redBits}{greenBits}{blueBits}";
            else if (blueShift > greenShift && greenShift > redShift)
                name = $"BGR{blueBits}{greenBits}{redBits}";
            else
                throw new UnexpectedDataException("The received pixel format is not in RGB or BGR order.");

            // Create pixel format without alpha support.
            // For some pixel formats, servers will send alpha values anyway, but we ignore them because that's how it's described in the protocol.
            return new PixelFormat($"RFB {name}", bitsPerPixel, depth, bigEndian, trueColor, false, redMax, greenMax, blueMax, 0, redShift, greenShift, blueShift, 0);
        }
    }
}
