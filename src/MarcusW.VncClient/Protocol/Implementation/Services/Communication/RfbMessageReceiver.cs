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
    /// A background thread that receives and processes RFB protocol messages.
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
            // Removed debug logging for production use
            Start();
        }

        /// <inheritdoc />
        public Task StopReceiveLoopAsync()
        {
            // Removed debug logging for production use
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
            
            // Removed debug logging for production use

            // Build a dictionary for faster lookup of incoming message types
            ImmutableDictionary<byte, IIncomingMessageType> incomingMessageLookup =
                _context.SupportedMessageTypes.OfType<IIncomingMessageType>().ToImmutableDictionary(mt => mt.Id);

            Span<byte> messageTypeBuffer = stackalloc byte[1];
            int messageCount = 0;

            _logger.LogDebug("Receive loop started. Waiting for messages...");

            while (!cancellationToken.IsCancellationRequested)
            {
                ++messageCount;
                
                // Read message type
                _logger.LogDebug("Waiting for message #{messageCount}...", messageCount);
                int bytesRead = transportStream.Read(messageTypeBuffer);
                
                if (bytesRead == 0)
                {
                    _logger.LogWarning("Stream returned 0 bytes (server closed connection) at message #{messageCount}.", messageCount);
                    throw new UnexpectedEndOfStreamException("Stream reached its end while reading next message type.");
                }
                byte messageTypeId = messageTypeBuffer[0];

                // Find message type
                if (!incomingMessageLookup.TryGetValue(messageTypeId, out IIncomingMessageType messageType))
                {
                    _logger.LogWarning("Server sent unsupported message type {MessageTypeId} at message #{messageCount}. This is likely a protocol extension that this client doesn't support " +
                        "or the protocol stream got desynchronized. The connection will be interrupted to trigger a reconnect.", messageTypeId, messageCount);

                    // We can't safely skip unknown message types since we don't know their length.
                    // Continuing could cause protocol stream desynchronization.
                    // Throwing here raises the thread's Failed event, which marks the connection
                    // as interrupted and triggers a reconnect (a plain "return" would end the
                    // receive loop silently and leave the connection stuck in the Connected state).
                    throw new UnexpectedDataException($"Server sent a message of unsupported type {messageTypeId}. "
                        + "Closing the connection to avoid protocol stream desynchronization.");
                }

                _logger.LogDebug("Received message #{messageCount}: {messageName} (id={messageTypeId}).", messageCount, messageType.Name, messageTypeId);

                // Ensure the message type is marked as used
                if (!messageType.IsStandardMessageType)
                    _state.EnsureMessageTypeIsMarkedAsUsed(messageType);

                // Read the message
                messageType.ReadMessage(transport, cancellationToken);
                _logger.LogDebug("Finished processing message #{messageCount}: {messageName}.", messageCount, messageType.Name);
            }
            _logger.LogDebug("Receive loop ended (cancellation requested). Total messages processed: {messageCount}.", messageCount);
        }
    }
}
