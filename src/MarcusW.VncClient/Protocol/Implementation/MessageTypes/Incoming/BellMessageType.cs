using System;
using System.Threading;
using MarcusW.VncClient.Output;
using MarcusW.VncClient.Protocol.MessageTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Incoming
{
    /// <summary>
    /// A message type for receiving a server bell message.
    /// </summary>
    public class BellMessageType : IIncomingMessageType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<BellMessageType> _logger;
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownIncomingMessageType.Bell;

        /// <inheritdoc />
        public string Name => "Bell";

        /// <inheritdoc />
        public bool IsStandardMessageType => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="BellMessageType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public BellMessageType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<BellMessageType>();
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public void ReadMessage(ITransport transport, CancellationToken cancellationToken = default)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Beep! https://www.youtube.com/watch?v=CZlfbep2LdU");

            _context.Connection.OutputHandler?.RingBell();
        }
    }
}
