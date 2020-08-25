using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;
using MarcusW.VncClient.Protocol.MessageTypes;
using MarcusW.VncClient.Rendering;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Incoming
{
    /// <summary>
    /// A message type for receiving FramebufferUpdate messages and rendering the contained rectangles to the framebuffer.
    /// </summary>
    public class FramebufferUpdateMessageType : IIncomingMessageType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<FramebufferUpdateMessageType> _logger;
        private readonly ProtocolState _state;

        private readonly IImmutableDictionary<int, (IEncodingType encodingType, bool usedPreviously)> _encodingTypesLookup;

        private readonly Stopwatch _stopwatch = new Stopwatch();

        // Common buffer for all read operations in this method
        private readonly byte[] _buffer = new byte[4 * sizeof(ushort) + sizeof(int)];

        private int? _lastEncodingTypeId;
        private IEncodingType? _lastEncodingType;

        /// <inheritdoc />
        public byte Id => 0;

        /// <inheritdoc />
        public string Name => "FramebufferUpdate";

        /// <inheritdoc />
        public bool IsStandardMessageType => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="FramebufferUpdateMessageType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public FramebufferUpdateMessageType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<FramebufferUpdateMessageType>();
            _state = context.GetState<ProtocolState>();

            // Build a dictionary for fast lookup of encoding types
            _encodingTypesLookup = context.SupportedEncodingTypes.ToImmutableDictionary(et => et.Id, et => (et, false));
        }

        /// <inheritdoc />
        public void ReadMessage(ITransport transport, CancellationToken cancellationToken = default)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));

            cancellationToken.ThrowIfCancellationRequested();

            Stream transportStream = transport.Stream;
            Span<byte> buffer = _buffer.AsSpan();

            // Read 3 header bytes
            transportStream.ReadAll(buffer.Slice(0, 3), cancellationToken);

            // Read number of rectangles (first byte is padding)
            ushort numberOfRectangles = BinaryPrimitives.ReadUInt16BigEndian(buffer[1..]);
            if (numberOfRectangles == 0)
                return;

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Receiving framebuffer update with "
                    + (numberOfRectangles == 65535 ? "a dynamic amount of rectangles..." : $"{numberOfRectangles} rectangles..."));
                _stopwatch.Restart();
            }

            // Cache for remote framebuffer information. This assumes that the framebuffer size and format properties are only changed by received messages/pseudo-encodings.
            var remoteFramebufferInfoChanged = true;
            Size remoteFramebufferSize = default;
            PixelFormat remoteFramebufferFormat = default;

            // Read rectangles
            ushort rectanglesRead;
            for (rectanglesRead = 0; rectanglesRead < numberOfRectangles; rectanglesRead++)
            {
                transportStream.ReadAll(buffer, cancellationToken);

                // Read encoding type first
                int encodingTypeId = BinaryPrimitives.ReadInt32BigEndian(buffer[8..]);

                IEncodingType encodingType;

                // Skip lookup in case we receive the same encoding type multiple times
                if (_lastEncodingTypeId == encodingTypeId)
                {
                    encodingType = _lastEncodingType!;
                }
                else
                {
                    // Lookup encoding type and remember it for next time
                    encodingType = _lastEncodingType = LookupEncodingType(encodingTypeId);
                    _lastEncodingTypeId = encodingTypeId;
                }

                if (encodingType is IFrameEncodingType frameEncodingType)
                {
                    // Read rectangle information
                    ushort x = BinaryPrimitives.ReadUInt16BigEndian(buffer);
                    ushort y = BinaryPrimitives.ReadUInt16BigEndian(buffer[2..]);
                    ushort width = BinaryPrimitives.ReadUInt16BigEndian(buffer[4..]);
                    ushort height = BinaryPrimitives.ReadUInt16BigEndian(buffer[6..]);
                    Rectangle rectangle = new Rectangle(x, y, width, height);

                    // Update remote framebuffer information
                    if (remoteFramebufferInfoChanged)
                    {
                        // These properties are synchronized, so retrieving the values might take a bit longer.
                        remoteFramebufferSize = _state.RemoteFramebufferSize;
                        remoteFramebufferFormat = _state.RemoteFramebufferFormat;

                        remoteFramebufferInfoChanged = false;
                    }

                    // Get render target (cannot be cached because it could change at any time)
                    IRenderTarget? renderTarget = _context.Connection.RenderTarget;

                    // Read frame encoding
                    frameEncodingType.ReadFrameEncoding(transportStream, renderTarget, rectangle, remoteFramebufferSize, remoteFramebufferFormat);
                }
                else
                {
                    throw new NotImplementedException();

                    // TODO: ref framebufferInfoChanged -> set to true if changed

                    // TODO: is ILastRectPsuedoEncodingType --> break
                }
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _stopwatch.Stop();
                _logger.LogDebug("Received and rendered {rectangles} rectangles in {milliseconds}ms.", rectanglesRead, _stopwatch.ElapsedMilliseconds);
            }

            // Ensure more framebuffer updates are coming
            RequestNextFramebufferUpdate();
        }

        private IEncodingType LookupEncodingType(in int id)
        {
            // Lookup encoding type
            if (!_encodingTypesLookup.TryGetValue(id, out (IEncodingType encodingType, bool usedPreviously) lookupEntry))
                throw new UnexpectedDataException($"Server sent an encoding of type {id} ({id:x8}) that is not supported by this protocol implementation. "
                    + "Servers should always check for client support before using protocol extensions.");

            // Is this the first use of this encoding type and do we need to mark it as used?
            if (!lookupEntry.usedPreviously && lookupEntry.encodingType.GetsConfirmed)
            {
                // Ensure the encoding type is marked as used
                IImmutableSet<IEncodingType> usedEncodingTypes = _state.UsedEncodingTypes;
                if (!usedEncodingTypes.Contains(lookupEntry.encodingType))
                    _state.UsedEncodingTypes = usedEncodingTypes.Add(lookupEntry.encodingType);
            }

            // Remember, that it was used at least once so we can skip updating the used encoding types next time
            lookupEntry.usedPreviously = true;

            return lookupEntry.encodingType;
        }

        private void RequestNextFramebufferUpdate()
        {
            // Will this happen automatically?
            if (_state.ContinuousUpdatesEnabled)
                return;

            // TODO: Enable continuous updates if supported.

            // Request next incremental update
            Debug.Assert(_context.MessageSender != null, "_context.MessageSender != null");
            _context.MessageSender.EnqueueMessage(new FramebufferUpdateRequestMessage(true, new Rectangle(Position.Origin, _state.RemoteFramebufferSize)));
        }
    }
}
