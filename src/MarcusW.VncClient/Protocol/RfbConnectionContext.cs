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

        internal TcpClient? TcpClient { get; set; }

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
        /// Gets the base stream for receiving and sending server messages.
        /// Should only be used inside of custom protocol implementation classes.
        /// </summary>
        public Stream Stream
            => TcpClient?.GetStream() ?? throw new InvalidOperationException("Stream is not accessible yet.");

        public RfbProtocolVersion ProtocolVersion
            => HandshakeResult?.ProtocolVersion
                ?? throw new InvalidOperationException("Protocol handshake has not completed yet.");

        internal RfbConnectionContext(RfbConnection connection)
        {
            Connection = connection;
        }
    }
}
