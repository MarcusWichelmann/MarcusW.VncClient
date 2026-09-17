using System;
using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding type for receiving QEMU Pointer Motion Change updates from the server.
    /// </summary>
    /// <remarks>
    /// This extension allows the server to switch the client between absolute and relative
    /// pointer modes. In relative mode, the client sends deltas instead of absolute coordinates,
    /// which is important for VMs with devices that expect relative coordinates (e.g., PS/2 mouse).
    /// </remarks>
    public class QemuPointerMotionChangeEncodingType : PseudoEncodingType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<QemuPointerMotionChangeEncodingType> _logger;
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.QemuPointerMotionChange;

        /// <inheritdoc />
        public override string Name => "QEMU Pointer Motion Change";

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="QemuPointerMotionChangeEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public QemuPointerMotionChangeEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<QemuPointerMotionChangeEncodingType>();
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
        {
            // The server sends a pseudo-rectangle where:
            // x-position = 1: requesting absolute coordinates (RFB default)
            // x-position = 0: requesting relative deltas
            _state.ServerSupportsQemuPointerMotionChange = true;

            bool relativeMode = rectangle.Position.X == 0;
            _state.RelativePointerMode = relativeMode;

            _logger.LogDebug("Server set pointer mode to {Mode}", relativeMode ? "relative" : "absolute");

            // Notify the output handler about the mode change
            _context.Connection.OutputHandler?.HandlePointerModeChange(relativeMode);
        }
    }
}
