using System;
using System.Collections.Immutable;
using MarcusW.VncClient.Protocol.Encodings;
using MarcusW.VncClient.Protocol.Messages;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Protocol.Services;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// Holds multiple connection-related objects and acts as a central place to provide the protocol implementation classes access to them.
    /// </summary>
    /// <remarks>
    /// This is just a container class that does not implement any complex logic.
    /// Remember that some of the properties might be uninitialized during early stages of the protocol.
    /// </remarks>
    public class RfbConnectionContext
    {
        /// <summary>
        /// Gets the <see cref="RfbConnection"/> to which this connection belongs to.
        /// </summary>
        public RfbConnection Connection { get; }

        /// <summary>
        /// Gets a <see cref="ConnectionDetailsAccessor"/> which provides write access to some details properties on the <see cref="RfbConnection"/> object.
        /// </summary>
        public ConnectionDetailsAccessor ConnectionDetails { get; }

        /// <summary>
        /// Gets the protocol state object.
        /// </summary>
        public IRfbProtocolState State { get; internal set; }

        /// <summary>
        /// Gets the security types that are supported by the client.
        /// </summary>
        public IImmutableDictionary<byte, ISecurityType>? SupportedSecurityTypes { get; internal set; }

        /// <summary>
        /// Gets the messages that are supported by the client.
        /// </summary>
        public IImmutableDictionary<byte, IMessage>? SupportedMessages { get; internal set; }

        /// <summary>
        /// Gets the encodings that are supported by the client.
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
        /// Gets the result of the connection initialization.
        /// </summary>
        public InitializationResult? InitializationResult { get; internal set; }

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
        public RfbProtocolVersion ProtocolVersion => HandshakeResult?.ProtocolVersion ?? RfbProtocolVersion.Unknown;

        internal RfbConnectionContext(RfbConnection connection)
        {
            Connection = connection;
            ConnectionDetails = new ConnectionDetailsAccessor(connection);
        }

        /// <summary>
        /// Provides the protocol implementation classes with write access to some details properties of the <see cref="RfbConnection"/> object.
        /// </summary>
        public class ConnectionDetailsAccessor
        {
            private readonly RfbConnection _connection;

            internal ConnectionDetailsAccessor(RfbConnection connection)
            {
                _connection = connection;
            }

            /// <summary>
            /// Sets the value of the <seealso cref="RfbConnection.UsedMessages"/> property on the <see cref="RfbConnection"/> object.
            /// </summary>
            /// <param name="usedMessages">The new messages set.</param>
            public void SetUsedMessages(IImmutableDictionary<byte, IMessage> usedMessages) => _connection.UsedMessages = usedMessages;

            /// <summary>
            /// Sets the value of the <seealso cref="RfbConnection.UsedEncodings"/> property on the <see cref="RfbConnection"/> object.
            /// </summary>
            /// <param name="usedEncodings">The new encodings set.</param>
            public void SetUsedEncodings(IImmutableDictionary<int, IEncoding> usedEncodings) => _connection.UsedEncodings = usedEncodings;

            /// <summary>
            /// Sets the value of the <seealso cref="RfbConnection.FramebufferSize"/> property on the <see cref="RfbConnection"/> object.
            /// </summary>
            /// <param name="framebufferSize">The new framebuffer size.</param>
            public void SetFramebufferSize(FrameSize framebufferSize) => _connection.FramebufferSize = framebufferSize;

            /// <summary>
            /// Sets the value of the <seealso cref="RfbConnection.FramebufferFormat"/> property on the <see cref="RfbConnection"/> object.
            /// </summary>
            /// <param name="framebufferFormat">The new framebuffer format.</param>
            public void SetFramebufferFormat(PixelFormat framebufferFormat) => _connection.FramebufferFormat = framebufferFormat;

            /// <summary>
            /// Sets the value of the <seealso cref="RfbConnection.DesktopName"/> property on the <see cref="RfbConnection"/> object.
            /// </summary>
            /// <param name="desktopName">The new desktop name.</param>
            public void SetDesktopName(string desktopName) => _connection.DesktopName = desktopName;
        }
    }
}
