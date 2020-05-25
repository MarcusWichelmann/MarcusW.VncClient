using System;
using System.Collections.Generic;
using MarcusW.VncClient.Protocol.Encodings;
using MarcusW.VncClient.Protocol.Services.Communication;
using MarcusW.VncClient.Protocol.Services.Connection;
using MarcusW.VncClient.Protocol.Services.Handshaking;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// Default implementation of the RFB protocol.
    /// </summary>
    public class RfbProtocol : IRfbProtocolImplementation
    {
        /// <inheritdoc />
        public IReadOnlyCollection<IEncoding> SupportedEncodings { get; }

        internal RfbProtocol(IReadOnlyCollection<IEncoding> supportedEncodings)
        {
            SupportedEncodings = supportedEncodings;
        }

        /// <inheritdoc />
        public ITcpConnector CreateTcpConnector(RfbConnectionContext context) => new TcpConnector(context);

        /// <inheritdoc />
        public IRfbHandshaker CreateRfbHandshaker(RfbConnectionContext context) => new RfbHandshaker(context);

        /// <inheritdoc />
        public IRfbMessageReceiver CreateMessageReceiver(RfbConnectionContext context)
            => new RfbMessageReceiver(context);

        /// <inheritdoc />
        public IRfbMessageSender CreateMessageSender(RfbConnectionContext context) => new RfbMessageSender();
    }
}
