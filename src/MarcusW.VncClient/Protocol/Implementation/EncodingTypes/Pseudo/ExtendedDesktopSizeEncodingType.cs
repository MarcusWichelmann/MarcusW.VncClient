using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;
using MarcusW.VncClient.Protocol.MessageTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding type to receive changes of the remote framebuffer size and screen layout.
    /// </summary>
    public class ExtendedDesktopSizeEncodingType : PseudoEncodingType
    {
        private enum ChangeReason
        {
            ServerSideChangeOrStatusUpdate,
            ChangeByCurrentClient,
            ChangeByOtherClient
        }

        private readonly RfbConnectionContext _context;
        private readonly ILogger<ExtendedDesktopSizeEncodingType> _logger;
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.ExtendedDesktopSize;

        /// <inheritdoc />
        public override string Name => "ExtendedDesktopSize";

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedDesktopSizeEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public ExtendedDesktopSizeEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<ExtendedDesktopSizeEncodingType>();
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
        {
            // Did we just learn that the server supports this extension?
            if (!_state.ServerSupportsExtendedDesktopSize)
            {
                _logger.LogDebug("Server supports the extended desktop size extension.");

                // Mark the SetDesktopSize message as used
                _state.EnsureMessageTypeIsMarkedAsUsed<IOutgoingMessageType>(null, (byte)WellKnownOutgoingMessageType.SetDesktopSize);

                _state.ServerSupportsExtendedDesktopSize = true;
            }

            // Get the reason why this encoding type was received
            ChangeReason changeReason = rectangle.Position.X switch {
                1     => ChangeReason.ChangeByCurrentClient,
                2     => ChangeReason.ChangeByOtherClient,
                var _ => ChangeReason.ServerSideChangeOrStatusUpdate
            };

            // If the client previously requested a size change that failed, the y coordinate contains the status code
            int statusCode = rectangle.Position.Y;
            if (changeReason == ChangeReason.ChangeByCurrentClient && statusCode != 0)
            {
                _logger.LogWarning("Server responded to a previously requested desktop size change with an error: " + statusCode switch {
                    1     => "Resize is administratively prohibited",
                    2     => "Out of resources",
                    3     => "Invalid screen layout",
                    var _ => $"Unknown failure, status code: {statusCode}"
                });
            }

            // The size of the pseudo rectangle is the new size
            Size newSize = rectangle.Size;

            // Common buffer for the following read operations
            Span<byte> readBuffer = stackalloc byte[sizeof(uint) + 4 * sizeof(ushort) + sizeof(uint)];

            // Read number of screens (and 3 bytes for padding)
            Span<byte> headerSpan = readBuffer.Slice(0, 4);
            transportStream.ReadAll(headerSpan);
            int numberOfScreens = headerSpan[0];

            // Read screens
            var screens = new Screen[numberOfScreens];
            for (var i = 0; i < numberOfScreens; i++)
            {
                transportStream.ReadAll(readBuffer);
                uint id = BinaryPrimitives.ReadUInt32BigEndian(readBuffer);
                ushort x = BinaryPrimitives.ReadUInt16BigEndian(readBuffer[4..]);
                ushort y = BinaryPrimitives.ReadUInt16BigEndian(readBuffer[6..]);
                ushort width = BinaryPrimitives.ReadUInt16BigEndian(readBuffer[8..]);
                ushort height = BinaryPrimitives.ReadUInt16BigEndian(readBuffer[10..]);
                uint flags = BinaryPrimitives.ReadUInt32BigEndian(readBuffer[12..]);
                screens[i] = new Screen(id, new Rectangle(x, y, width, height), flags);
            }

            // Check if all screen ids are unique
            if (screens.Select(s => s.Id).Distinct().Count() != numberOfScreens)
                throw new UnexpectedDataException("At least two of the received framebuffer screens have conflicting IDs. This is not allowed.");

            // Check if all screens are contained by the framebuffer size
            if (screens.Any(s => !s.Rectangle.FitsInside(newSize)))
                throw new UnexpectedDataException("At least one of the received framebuffer screens lies (partially) outside of the framebuffer area.");

            if (newSize == _state.RemoteFramebufferSize && screens.SequenceEqual(_state.RemoteFramebufferLayout))
                return;

            _logger.LogDebug("Remote framebuffer size updated to {newSize} with a {screenCount}-screen layout: {layout}. Reason: {changeReason}", newSize, numberOfScreens,
                string.Join(", ", screens), changeReason);

            // Set the new framebuffer size and layout
            _state.RemoteFramebufferSize = newSize;
            _state.RemoteFramebufferLayout = screens.ToImmutableHashSet();

            Debug.Assert(_context.MessageSender != null, "_context.MessageSender != null");
            var wholeScreenRectangle = new Rectangle(Position.Origin, newSize);

            // If continuous updates were enabled, enable it again to update it's coordinates.
            // If they were not previously enabled, the server seems to send a full FramebufferUpdate on its own. We cannot request a non-incremental update manually,
            // because this would conflict with the support-check for the ExtendedDesktopSize extension and cause an infinite loop.
            // See protocol specification for details.
            if (_state.ServerSupportsContinuousUpdates && _state.ContinuousUpdatesEnabled)
                _context.MessageSender.EnqueueMessage(new EnableContinuousUpdatesMessage(true, wholeScreenRectangle));
        }
    }
}
