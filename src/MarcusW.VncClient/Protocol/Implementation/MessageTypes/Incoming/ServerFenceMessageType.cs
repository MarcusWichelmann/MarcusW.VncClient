using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;
using MarcusW.VncClient.Protocol.MessageTypes;
using MarcusW.VncClient.Protocol.Services;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Incoming
{
    /// <summary>
    /// A message type for receiving and responding to a server-side fence message.
    /// </summary>
    public class ServerFenceMessageType : IIncomingMessageType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<ServerFenceMessageType> _logger;
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownIncomingMessageType.ServerFence;

        /// <inheritdoc />
        public string Name => "ServerFence";

        /// <inheritdoc />
        public bool IsStandardMessageType => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerFenceMessageType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public ServerFenceMessageType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<ServerFenceMessageType>();
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public void ReadMessage(ITransport transport, CancellationToken cancellationToken = default)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));

            cancellationToken.ThrowIfCancellationRequested();

            // Did we just learn that the server supports fences?
            if (!_state.ServerSupportsFences)
            {
                _logger.LogDebug("Server supports the fences extension.");

                // Mark the encoding and message type as used
                _state.EnsureEncodingTypeIsMarkedAsUsed<IPseudoEncodingType>(null, (int)WellKnownEncodingType.Fence);
                _state.EnsureMessageTypeIsMarkedAsUsed<IOutgoingMessageType>(null, (byte)WellKnownOutgoingMessageType.ClientFence);

                _state.ServerSupportsFences = true;
            }

            Stream transportStream = transport.Stream;

            // Read 8 header bytes (first 3 bytes are padding)
            Span<byte> header = stackalloc byte[8];
            transportStream.ReadAll(header, cancellationToken);
            var flags = (FenceFlags)BinaryPrimitives.ReadUInt32BigEndian(header[3..]);
            byte payloadLength = header[7];

            // Read payload, if any
            byte[] payload;
            if (payloadLength > 0)
            {
                if (payloadLength > 64)
                    throw new UnexpectedDataException("Payload size in fence messages is limited to 64 bytes.");

                // Allocate a new array on the heap, because we have to pass it to the send queue
                payload = new byte[payloadLength];
                transportStream.ReadAll(payload, cancellationToken);
            }
            else
            {
                payload = Array.Empty<byte>();
            }

            // Print a nice log message
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (payloadLength > 0)
                    _logger.LogDebug("Received server fence ({flags}) with {payloadLength} byte(s) of payload: {payload}", flags.ToString(), payloadLength,
                        BitConverter.ToString(payload));
                else
                    _logger.LogDebug("Received server fence ({flags}) with no payload.", flags.ToString());
            }

            // Leave only supported bits and clear the request bit
            // TODO: Implement SyncNext flag as soon as I find a server that uses it.
            flags &= FenceFlags.BlockBefore | FenceFlags.BlockAfter;

            // NOTE: The BlockBefore flag can be ignored here, because the processing of incoming messages is sequential anyway.

            // Create the fence response message
            var responseMessage = new ClientFenceMessage(flags, payload);

            Debug.Assert(_context.MessageSender != null, "_context.MessageSender != null");
            IRfbMessageSender messageSender = _context.MessageSender;

            // Should the receive loop be blocked until the fence response was send?
            if ((flags & FenceFlags.BlockAfter) != 0)
                messageSender.SendMessageAndWait(responseMessage, cancellationToken);
            else
                messageSender.EnqueueMessage(responseMessage, cancellationToken);
        }
    }
}
