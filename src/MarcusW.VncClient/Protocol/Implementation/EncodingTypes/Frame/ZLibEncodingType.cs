using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Frame
{
    /// <summary>
    /// A frame encoding type for zlib compressed raw pixel data.
    /// </summary>
    public class ZLibEncodingType : FrameEncodingType
    {
        private readonly RfbConnectionContext _context;

        private IFrameEncodingType? _rawEncodingType;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.ZLib;

        /// <inheritdoc />
        public override string Name => "ZLib";

        /// <inheritdoc />
        public override int Priority => 10;

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZLibEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public ZLibEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public override void ReadFrameEncoding(Stream transportStream, IFramebufferReference? targetFramebuffer, in Rectangle rectangle, in Size remoteFramebufferSize,
            in PixelFormat remoteFramebufferFormat)
        {
            // Ensure we have access to the raw encoding type
            if (_rawEncodingType == null)
            {
                Debug.Assert(_context.SupportedEncodingTypes != null, "_context.SupportedEncodingTypes != null");
                _rawEncodingType = _context.SupportedEncodingTypes.OfType<IFrameEncodingType>().FirstOrDefault(et => et.Id == (int)WellKnownEncodingType.Raw);
                if (_rawEncodingType == null)
                    throw new InvalidOperationException(
                        $"The ZLib encoding type is based on the Raw encoding type (ID {WellKnownEncodingType.Raw}), but it could not be found in the supported encoding types collection.");
            }

            // Read header with data length
            Span<byte> header = stackalloc byte[4];
            transportStream.ReadAll(header);
            uint dataLength = BinaryPrimitives.ReadUInt32BigEndian(header);

            // Create stream for inflating the data
            Debug.Assert(_context.ZLibInflater != null, "_context.ZLibInflater != null");
            Stream inflateStream = _context.ZLibInflater.ReadAndInflate(transportStream, (int)dataLength);

            _rawEncodingType.ReadFrameEncoding(inflateStream, targetFramebuffer, rectangle, remoteFramebufferSize, remoteFramebufferFormat);

            // TODO: During tests with vino VNC server (EOL), this encoding was a bit unstable after a few received frames because of the DeflateStream
            // throwing InvalidDataExeptions. Time has to show, if this is also the case with more current VNC servers like TigerVNC.
        }
    }
}
