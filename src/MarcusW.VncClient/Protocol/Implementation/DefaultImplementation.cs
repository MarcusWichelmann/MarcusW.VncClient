using System.Collections.Generic;
using System.Collections.Immutable;
using MarcusW.VncClient.Protocol.Encodings;
using MarcusW.VncClient.Protocol.Implementation.Services.Communication;
using MarcusW.VncClient.Protocol.Implementation.Services.Handshaking;
using MarcusW.VncClient.Protocol.Implementation.Services.Transports;
using MarcusW.VncClient.Protocol.Services;

namespace MarcusW.VncClient.Protocol.Implementation
{
    /// <summary>
    /// Default implementation of the RFB protocol.
    /// </summary>
    public class DefaultImplementation : IRfbProtocolImplementation
    {
        /// <inheritdoc />
        // TODO: More efficient data type?
        public IReadOnlyCollection<IEncoding> SupportedEncodings { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultImplementation"/>.
        /// </summary>
        public DefaultImplementation() : this(GetDefaultEncodings().ToImmutableList()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultImplementation"/>.
        /// </summary>
        /// <param name="supportedEncodings">The encodings that are supported by this instance. See <see cref="GetDefaultEncodings"/>.</param>
        public DefaultImplementation(IReadOnlyCollection<IEncoding> supportedEncodings)
        {
            SupportedEncodings = supportedEncodings;
        }

        /// <inheritdoc />
        public ITransportConnector CreateTransportConnector(RfbConnectionContext context) => new TransportConnector(context);

        /// <inheritdoc />
        public IRfbHandshaker CreateRfbHandshaker(RfbConnectionContext context) => new RfbHandshaker(context);

        /// <inheritdoc />
        public IRfbMessageReceiver CreateMessageReceiver(RfbConnectionContext context) => new RfbMessageReceiver(context);

        /// <inheritdoc />
        public IRfbMessageSender CreateMessageSender(RfbConnectionContext context) => new RfbMessageSender(context);

        /// <summary>
        /// Builds a collection with all RFB encodings that are officially supported by this protocol implementation.
        /// Feel free to extend the returned enumerable with custom encodings.
        /// </summary>
        /// <returns>The encoding collection.</returns>
        public static IEnumerable<IEncoding> GetDefaultEncodings()
        {
            // TODO: Add encodings
            yield break;
        }
    }
}
