using System;
using System.Collections.Generic;
using MarcusW.VncClient.Protocol.Encodings;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// Default implementation of the RFB protocol that provides access to different elements of a RFB protocol
    /// implementation.
    /// </summary>
    internal class RfbProtocol : IRfbProtocolImplementation
    {
        public IReadOnlyCollection<IEncoding> SupportedEncodings { get; }

        public RfbProtocol(IReadOnlyCollection<IEncoding> supportedEncodings)
        {
            SupportedEncodings = supportedEncodings ?? throw new ArgumentNullException(nameof(supportedEncodings));
        }

        public IRfbMessageReceiver CreateMessageReceiver(RfbConnection connection)
            => new RfbMessageReceiver(connection);

        public IRfbMessageSender CreateMessageSender(RfbConnection connection) => new RfbMessageSender();
    }
}
