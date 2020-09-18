using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo;
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

        private readonly bool _lockTargetByRectangle;

        private readonly Stopwatch _stopwatch = new Stopwatch();

        // Common buffer for all read operations in this method
        private readonly byte[] _buffer = new byte[4 * sizeof(ushort) + sizeof(int)];

        private int? _lastEncodingTypeId;
        private IEncodingType? _lastEncodingType;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownIncomingMessageType.FramebufferUpdate;

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

            // Build a dictionary for faster lookup of encoding types
            _encodingTypesLookup = context.SupportedEncodingTypes.ToImmutableDictionary(et => et.Id, et => (et, false));

            // Should the target framebuffer be locked by rectangle or by frame?
            _lockTargetByRectangle = context.Connection.Parameters.RenderFlags.HasFlag(RenderFlags.UpdateByRectangle);
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
            Size remoteFramebufferSize = _state.RemoteFramebufferSize;
            PixelFormat remoteFramebufferFormat = _state.RemoteFramebufferFormat;

            // Get the current render target (can be null)
            IRenderTarget? renderTarget = _context.Connection.RenderTarget;
            IFramebufferReference? targetFramebuffer = null;

            ushort rectanglesRead;
            try
            {
                // Read rectangles
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

                        // Lock the target framebuffer, if there is no reference yet
                        if (renderTarget != null && targetFramebuffer == null)
                        {
                            targetFramebuffer = renderTarget.GrabFramebufferReference(remoteFramebufferSize);
                            if (targetFramebuffer.Size != remoteFramebufferSize)
                                throw new RfbProtocolException("Framebuffer reference is not of the requested size.");
                        }

                        try
                        {
                            // Read frame encoding
                            frameEncodingType.ReadFrameEncoding(transportStream, targetFramebuffer, rectangle, remoteFramebufferSize, remoteFramebufferFormat);
                        }
                        finally
                        {
                            // Release the framebuffer reference if per-rectangle updates are enabled so a new one gets requested for the next rectangle.
                            if (_lockTargetByRectangle)
                            {
                                targetFramebuffer?.Dispose();
                                targetFramebuffer = null;
                            }
                        }
                    }
                    else if (encodingType is IPseudoEncodingType pseudoEncodingType)
                    {
                        // Stop after a LastRect encoding
                        if (encodingType is ILastRectEncodingType)
                            break;

                        // Ignore the rectangle information and just call the pseudo encoding
                        pseudoEncodingType.ReadPseudoEncoding(transportStream);

                        // The pseudo encoding might have changed the cached framebuffer information.
                        remoteFramebufferSize = _state.RemoteFramebufferSize;
                        remoteFramebufferFormat = _state.RemoteFramebufferFormat;
                    }
                }
            }
            finally
            {
                // Release any remaining framebuffer reference
                targetFramebuffer?.Dispose();
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _stopwatch.Stop();
                _logger.LogDebug("Received and rendered/processed {rectangles} rectangles in {milliseconds}ms. Please note that debug builds are way less optimized.",
                    rectanglesRead, _stopwatch.ElapsedMilliseconds);
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

            // Ensure the encoding type is marked as used
            if (!lookupEntry.usedPreviously && lookupEntry.encodingType.GetsConfirmed)
                _state.EnsureEncodingTypeIsMarkedAsUsed(lookupEntry.encodingType);

            // Remember, that it was used at least once so we can skip updating the used encoding types next time
            lookupEntry.usedPreviously = true;

            return lookupEntry.encodingType;
        }

        private void RequestNextFramebufferUpdate()
        {
            // Will this happen automatically?
            if (_state.ContinuousUpdatesEnabled)
                return;

            Debug.Assert(_context.MessageSender != null, "_context.MessageSender != null");

            var wholeScreenRectangle = new Rectangle(Position.Origin, _state.RemoteFramebufferSize);

            // Can we enable continuous updates instead of requesting single updates?
            if (_state.ServerSupportsContinuousUpdates)
            {
                _context.MessageSender.EnqueueMessage(new EnableContinuousUpdatesMessage(true, wholeScreenRectangle));
                _state.ContinuousUpdatesEnabled = true;
                return;
            }

            // Request next incremental update
            _context.MessageSender.EnqueueMessage(new FramebufferUpdateRequestMessage(true, wholeScreenRectangle));
        }
    }
}
