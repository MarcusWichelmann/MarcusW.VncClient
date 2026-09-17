using System;
using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding which declares support for the Extended Clipboard extension.
    /// </summary>
    /// <remarks>
    /// The Extended Clipboard extension provides rich clipboard support including
    /// text, RTF, HTML, images (DIB), and file transfers. It modifies the behavior
    /// of ClientCutText and ServerCutText messages.
    /// </remarks>
    public class ExtendedClipboardEncodingType : PseudoEncodingType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<ExtendedClipboardEncodingType> _logger;
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.ExtendedClipboard;

        /// <inheritdoc />
        public override string Name => "Extended Clipboard";

        /// <inheritdoc />
        public override bool GetsConfirmed => false; // Server confirms via caps message

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedClipboardEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public ExtendedClipboardEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<ExtendedClipboardEncodingType>();
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
        {
            // This is a marker pseudo-encoding. The server confirms support
            // by sending a ServerCutText message with caps flag set.
            _logger.LogDebug("Client declared Extended Clipboard support");
        }
    }
}
