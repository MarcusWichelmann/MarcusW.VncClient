using System;
using System.Threading;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;
using MarcusW.VncClient.Protocol.MessageTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Incoming
{
    /// <summary>
    /// A message type for processing the end of a continuous updates phase.
    /// </summary>
    public class EndOfContinuousUpdatesMessageType : IIncomingMessageType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<EndOfContinuousUpdatesMessageType> _logger;
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownIncomingMessageType.EndOfContinuousUpdates;

        /// <inheritdoc />
        public string Name => "EndOfContinuousUpdates";

        /// <inheritdoc />
        public bool IsStandardMessageType => false;

        public EndOfContinuousUpdatesMessageType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<EndOfContinuousUpdatesMessageType>();
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public void ReadMessage(ITransport transport, CancellationToken cancellationToken = default)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));

            cancellationToken.ThrowIfCancellationRequested();

            // Did we just learn that the server supports continuous updates?
            if (!_state.ServerSupportsContinuousUpdates)
            {
                _logger.LogDebug("Server supports continuous updates extension.");

                // Mark the encoding and message type as used
                _state.EnsureEncodingTypeIsMarkedAsUsed<IPseudoEncodingType>(null, (int)WellKnownEncodingType.ContinuousUpdates);
                _state.EnsureMessageTypeIsMarkedAsUsed<IOutgoingMessageType>(null, (byte)WellKnownOutgoingMessageType.EnableContinuousUpdates);

                _state.ServerSupportsContinuousUpdates = true;
            }

            // Continuous updates have ended
            _state.ContinuousUpdatesEnabled = false;
        }
    }
}
