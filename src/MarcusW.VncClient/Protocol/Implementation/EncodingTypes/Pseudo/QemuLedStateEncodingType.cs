using System;
using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// Represents the LED state flags from the server.
    /// </summary>
    [Flags]
    public enum LedState : byte
    {
        /// <summary>
        /// No LEDs are active.
        /// </summary>
        None = 0,

        /// <summary>
        /// Scroll Lock LED is on.
        /// </summary>
        ScrollLock = 1 << 0,

        /// <summary>
        /// Num Lock LED is on.
        /// </summary>
        NumLock = 1 << 1,

        /// <summary>
        /// Caps Lock LED is on.
        /// </summary>
        CapsLock = 1 << 2
    }

    /// <summary>
    /// A pseudo encoding type for receiving QEMU LED State updates from the server.
    /// </summary>
    /// <remarks>
    /// This extension allows the server to synchronize keyboard LED states (Caps Lock,
    /// Num Lock, Scroll Lock) with the client.
    /// </remarks>
    public class QemuLedStateEncodingType : PseudoEncodingType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<QemuLedStateEncodingType> _logger;
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.QemuLedState;

        /// <inheritdoc />
        public override string Name => "QEMU LED State";

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="QemuLedStateEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public QemuLedStateEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<QemuLedStateEncodingType>();
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
        {
            _state.ServerSupportsQemuLedState = true;

            // Read 1 byte of LED state
            Span<byte> buffer = stackalloc byte[1];
            transportStream.ReadAll(buffer);
            var ledState = (LedState)buffer[0];

            _logger.LogDebug("Received LED state: ScrollLock={ScrollLock}, NumLock={NumLock}, CapsLock={CapsLock}",
                ledState.HasFlag(LedState.ScrollLock),
                ledState.HasFlag(LedState.NumLock),
                ledState.HasFlag(LedState.CapsLock));

            // Notify the output handler about the LED state change
            _context.Connection.OutputHandler?.HandleLedStateChange(ledState);
        }
    }
}
