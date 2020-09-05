using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;
using MarcusW.VncClient.Protocol.MessageTypes;
using MarcusW.VncClient.Protocol.Services;
using MarcusW.VncClient.Rendering;
using MarcusW.VncClient.Utils;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Communication
{
    /// <summary>
    /// A Background thread that receives and processes RFB protocol messages.
    /// </summary>
    public sealed class RfbMessageReceiver : BackgroundThread, IRfbMessageReceiver
    {
        private readonly RfbConnectionContext _context;
        private readonly ProtocolState _state;
        private readonly ILogger<RfbMessageReceiver> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RfbMessageReceiver"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public RfbMessageReceiver(RfbConnectionContext context) : base("RFB Message Receiver")
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _state = context.GetState<ProtocolState>();
            _logger = context.Connection.LoggerFactory.CreateLogger<RfbMessageReceiver>();

            // Log failure events from background thread base
            Failed += (sender, args) => _logger.LogWarning(args.Exception, "Receive loop failed.");
        }

        /// <inheritdoc />
        public void StartReceiveLoop()
        {
            _logger.LogDebug("Starting receive loop...");
            Start();
        }

        /// <inheritdoc />
        public Task StopReceiveLoopAsync()
        {
            _logger.LogDebug("Stopping receive loop...");
            return StopAndWaitAsync();
        }

        // This method will not catch exceptions so the BackgroundThread base class will receive them,
        // raise a "Failure" and trigger a reconnect.
        protected override void ThreadWorker(CancellationToken cancellationToken)
        {
            // Get the transport stream so we don't have to call the getter every time
            Debug.Assert(_context.Transport != null, "_context.Transport != null");
            ITransport transport = _context.Transport;
            Stream transportStream = transport.Stream;

            // Build a dictionary for faster lookup of incoming message types
            ImmutableDictionary<byte, IIncomingMessageType> incomingMessageLookup =
                _context.SupportedMessageTypes.OfType<IIncomingMessageType>().ToImmutableDictionary(mt => mt.Id);

            Span<byte> messageTypeBuffer = stackalloc byte[1];

            while (!cancellationToken.IsCancellationRequested)
            {
                // Read message type
                if (transportStream.Read(messageTypeBuffer) == 0)
                    throw new UnexpectedEndOfStreamException("Stream reached its end while reading next message type.");
                byte messageTypeId = messageTypeBuffer[0];

                // Find message type
                if (!incomingMessageLookup.TryGetValue(messageTypeId, out IIncomingMessageType messageType))
                    throw new UnexpectedDataException($"Server sent a message of type {messageTypeId} that is not supported by this protocol implementation. "
                        + "Servers should always check for client support before using protocol extensions.");

                _logger.LogDebug("Received message: {name}({id})", messageType.Name, messageTypeId);

                // Ensure the message type is marked as used
                if (!messageType.IsStandardMessageType)
                {
                    _logger.LogDebug("Marking {messageType} as used after seeing it for the first time...", messageType.Name);
                    _state.MarkMessageTypeAsUsed(messageType);
                }

                // Read the message
                messageType.ReadMessage(transport, cancellationToken);
            }
        }
    }
}
