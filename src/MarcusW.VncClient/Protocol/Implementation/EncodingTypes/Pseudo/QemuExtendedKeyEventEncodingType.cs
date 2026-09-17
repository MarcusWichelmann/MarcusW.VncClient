using System;
using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding which declares support for the QEMU Extended Key Event extension.
    /// </summary>
    /// <remarks>
    /// The QEMU Extended Key Event extension allows the client to send raw keycodes (XT scancodes)
    /// in addition to keysyms. This is essential for virtual machine use cases where the
    /// VM needs to interpret key events independently of the client's locale-specific keymap.
    /// </remarks>
    public class QemuExtendedKeyEventEncodingType : PseudoEncodingType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<QemuExtendedKeyEventEncodingType> _logger;
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.QemuExtendedKeyEvent;

        /// <inheritdoc />
        public override string Name => "QEMU Extended Key Event";

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="QemuExtendedKeyEventEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public QemuExtendedKeyEventEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<QemuExtendedKeyEventEncodingType>();
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
        {
            // The server sends an empty pseudo-rectangle to confirm support
            _logger.LogDebug("Server supports QEMU Extended Key Event extension");
            _state.ServerSupportsQemuExtendedKeyEvent = true;
        }
    }
}
