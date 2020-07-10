using System;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Incoming
{
    public class FramebufferUpdateMessageType : IIncomingMessageType
    {
        private readonly RfbConnectionContext _context;
        private readonly ProtocolState _state;

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
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public void ReadMessage(ITransport transport)
        {
            throw new NotImplementedException();
        }
    }
}
