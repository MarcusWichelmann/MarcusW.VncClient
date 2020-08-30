using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo;
using MarcusW.VncClient.Protocol.MessageTypes;
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

            // Did we just learn that the server supports fences?
            if (!_state.ServerSupportsFences)
            {
                // Mark the encoding as used
                _state.MarkEncodingTypeAsUsed((int)WellKnownEncodingType.Fence);

                // TODO: Add ClientFence to used message types

                _state.ServerSupportsFences = true;
            }

            throw new NotImplementedException();
        }
    }
}
