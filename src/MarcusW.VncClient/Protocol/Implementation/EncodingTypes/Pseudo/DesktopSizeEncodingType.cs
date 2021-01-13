using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Incoming;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding type to receive changes of the remote framebuffer size.
    /// </summary>
    public class DesktopSizeEncodingType : PseudoEncodingType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<DesktopSizeEncodingType> _logger;
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.DesktopSize;

        /// <inheritdoc />
        public override string Name => "DesktopSize";

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="DesktopSizeEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public DesktopSizeEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<DesktopSizeEncodingType>();
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
        {
            // This encoding type must not be used when the extended desktop size extension is supported. Unfortunately some VNC servers *cough* UltraVNC *cough* don't
            // seem to care, so we try to handle this as good as possible here and only throw a warning instead of an exception.
            if (_state.ServerSupportsExtendedDesktopSize)
                _logger.LogWarning(
                    "The server sent the DesktopSize pseudo encoding type although both sides support the ExtendedDesktopSize protocol extension. This is against the RFB protocol!");

            Size newSize = rectangle.Size;
            if (newSize == _state.RemoteFramebufferSize)
                return;

            var wholeScreenRectangle = new Rectangle(Position.Origin, newSize);

            _logger.LogDebug("Remote framebuffer size updated to {newSize}.", newSize);

            // Set the new framebuffer size
            _state.RemoteFramebufferSize = newSize;

            // Reset the screen layout. In case of VNC servers that are sending DesktopSize although they have used ExtendedDesktopSize before, this will break multi-monitor support,
            // but such VNC servers are broken anyway...
            _state.RemoteFramebufferLayout = new[] { new Screen(1, wholeScreenRectangle, 0) }.ToImmutableHashSet();

            Debug.Assert(_context.MessageSender != null, "_context.MessageSender != null");

            // If continuous updates were enabled, enable it again to update it's coordinates
            if (_state.ServerSupportsContinuousUpdates && _state.ContinuousUpdatesEnabled)
                _context.MessageSender.EnqueueMessage(new EnableContinuousUpdatesMessage(true, wholeScreenRectangle));

            // Request a whole-screen non-incremental update just to be sure. The protocol explicitly allows this.
            _context.MessageSender.EnqueueMessage(new FramebufferUpdateRequestMessage(false, wholeScreenRectangle));
        }
    }
}
