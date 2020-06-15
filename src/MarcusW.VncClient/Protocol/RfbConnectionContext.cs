using System;
using System.IO;
using System.Net.Sockets;
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
        /// Gets the current transport layer used by the protocol.
        /// Please note, that this might be replaced with tunnel transports during the handshake procedure.
        /// </summary>
        public ITransport? Transport { get; internal set; }

        /// <summary>
        /// Gets or sets the result of the initial handshake.
        /// </summary>
        public HandshakeResult? HandshakeResult { get; internal set; }

        /// <summary>
        /// Gets or sets the <see cref="IRfbMessageReceiver"/> for this connection.
        /// </summary>
        public IRfbMessageReceiver? MessageReceiver { get; internal set; }

        // TODO: Message sender etc...

        public RfbProtocolVersion ProtocolVersion => HandshakeResult?.ProtocolVersion ?? throw new InvalidOperationException("Protocol handshake has not completed yet.");

        internal RfbConnectionContext(RfbConnection connection)
        {
            Connection = connection;
        }
    }
}
