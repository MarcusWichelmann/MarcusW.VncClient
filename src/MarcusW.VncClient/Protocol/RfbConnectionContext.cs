using System;
using System.Collections.Immutable;
using MarcusW.VncClient.Protocol.Encodings;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Protocol.Services;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// Holds multiple connection-related objects and gives the protocol implementation classes access to some aspects of them.
    /// </summary>
    /// <remarks>
    /// This is just a container class that does not implement any complex logic.
    /// </remarks>
    public class RfbConnectionContext
    {
        /// <summary>
        /// Gets the <see cref="RfbConnection"/> to which this connection belongs to.
        /// </summary>
        public RfbConnection Connection { get; }

        /// <summary>
        /// Gets the supported security types.
        /// </summary>
        public IImmutableDictionary<byte, ISecurityType>? SupportedSecurityTypes { get; internal set; }

        /// <summary>
        /// Gets the supported encodings.
        /// </summary>
        public IImmutableDictionary<int, IEncoding>? SupportedEncodings { get; internal set; }

        /// <summary>
        /// Gets the current transport layer used by the protocol.
        /// Please note, that this might be replaced with tunnel transports during the handshake procedure.
        /// </summary>
        public ITransport? Transport { get; internal set; }

        /// <summary>
        /// Gets the result of the initial handshake.
        /// </summary>
        public HandshakeResult? HandshakeResult { get; internal set; }

        /// <summary>
        /// Gets the <see cref="IRfbMessageReceiver"/> for this connection.
        /// </summary>
        public IRfbMessageReceiver? MessageReceiver { get; internal set; }

        // TODO: Message sender etc...

        /// <summary>
        /// Gets the used RFB protocol implementation.
        /// </summary>
        public IRfbProtocolImplementation ProtocolImplementation => Connection.ProtocolImplementation;

        /// <summary>
        /// Gets the used protocol version.
        /// </summary>
        public RfbProtocolVersion ProtocolVersion => HandshakeResult?.ProtocolVersion ?? throw new InvalidOperationException("Protocol handshake has not completed yet.");

        internal RfbConnectionContext(RfbConnection connection)
        {
            Connection = connection;
        }
    }
}
