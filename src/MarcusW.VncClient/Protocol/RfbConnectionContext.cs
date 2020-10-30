using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.MessageTypes;
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
        public IRfbProtocolState? State { get; internal set; }

        /// <summary>
        /// Gets the security types that are supported by the client.
        /// </summary>
        public IImmutableSet<ISecurityType>? SupportedSecurityTypes { get; internal set; }

        /// <summary>
        /// Gets the message types that are supported by the client.
        /// </summary>
        public IImmutableSet<IMessageType>? SupportedMessageTypes { get; internal set; }

        /// <summary>
        /// Gets the encoding types that are supported by the client.
        /// </summary>
        public IImmutableSet<IEncodingType>? SupportedEncodingTypes { get; internal set; }

        /// <summary>
        /// Gets the current transport layer used by the protocol.
        /// Please note, that this might be replaced with tunnel transports during the handshake procedure.
        /// </summary>
        public ITransport? Transport { get; internal set; }

        /// <summary>
        /// Gets the <see cref="IRfbMessageReceiver"/> for this connection.
        /// </summary>
        public IRfbMessageReceiver? MessageReceiver { get; internal set; }

        /// <summary>
        /// Gets the <see cref="IRfbMessageSender"/> for this connection.
        /// </summary>
        public IRfbMessageSender? MessageSender { get; internal set; }

        /// <summary>
        /// Gets the <see cref="IZLibInflater"/> for this connection.
        /// </summary>
        public IZLibInflater? ZLibInflater { get; internal set; }

        /// <summary>
        /// Gets the <see cref="IImageDecoder"/> for this connection.
        /// </summary>
        public IImageDecoder? ImageDecoder { get; internal set; }

        internal RfbConnectionContext(RfbConnection connection)
        {
            Connection = connection;
            ConnectionDetails = new ConnectionDetailsAccessor(connection);
        }

        /// <summary>
        /// Casts the state object to <typeparam name="TState"/> and returns it.
        /// </summary>
        /// <typeparam name="TState">The type of the state object that implements <see cref="IRfbProtocolState"/>.</typeparam>
        /// <returns>The casted state.</returns>
        public TState GetState<TState>() where TState : class, IRfbProtocolState
        {
            if (State == null)
                throw new InvalidOperationException("State is not accessible yet.");

            return (TState)State;
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
            /// Sets the value of the <seealso cref="RfbConnection.ProtocolVersion"/> property on the <see cref="RfbConnection"/> object.
            /// </summary>
            /// <param name="protocolVersion">The new protocol version value.</param>
            public void SetProtocolVersion(RfbProtocolVersion protocolVersion) => _connection.ProtocolVersion = protocolVersion;

            /// <summary>
            /// Sets the value of the <seealso cref="RfbConnection.UsedSecurityType"/> property on the <see cref="RfbConnection"/> object.
            /// </summary>
            /// <param name="usedSecurityType">The new security type.</param>
            public void SetUsedSecurityType(ISecurityType? usedSecurityType) => _connection.UsedSecurityType = usedSecurityType;

            /// <summary>
            /// Sets the value of the <seealso cref="RfbConnection.UsedMessageTypes"/> property on the <see cref="RfbConnection"/> object.
            /// </summary>
            /// <param name="usedMessageTypes">The new message types set.</param>
            public void SetUsedMessageTypes(IImmutableSet<IMessageType> usedMessageTypes) => _connection.UsedMessageTypes = usedMessageTypes;

            /// <summary>
            /// Sets the value of the <seealso cref="RfbConnection.UsedEncodingTypes"/> property on the <see cref="RfbConnection"/> object.
            /// </summary>
            /// <param name="usedEncodingTypes">The new encoding types set.</param>
            public void SetUsedEncodingTypes(IImmutableSet<IEncodingType> usedEncodingTypes) => _connection.UsedEncodingTypes = usedEncodingTypes;

            /// <summary>
            /// Sets the value of the <seealso cref="RfbConnection.RemoteFramebufferSize"/> property on the <see cref="RfbConnection"/> object.
            /// </summary>
            /// <param name="remoteFramebufferSize">The new framebuffer size.</param>
            public void SetRemoteFramebufferSize(Size remoteFramebufferSize) => _connection.RemoteFramebufferSize = remoteFramebufferSize;

            /// <summary>
            /// Sets the value of the <seealso cref="RfbConnection.RemoteFramebufferFormat"/> property on the <see cref="RfbConnection"/> object.
            /// </summary>
            /// <param name="remoteFramebufferFormat">The new framebuffer format.</param>
            public void SetRemoteFramebufferFormat(PixelFormat remoteFramebufferFormat) => _connection.RemoteFramebufferFormat = remoteFramebufferFormat;

            /// <summary>
            /// Sets the value of the <seealso cref="RfbConnection.RemoteFramebufferLayout"/> property on the <see cref="RfbConnection"/> object.
            /// </summary>
            /// <param name="remoteFramebufferLayout">The new framebuffer layout.</param>
            public void SetRemoteFramebufferLayout(IImmutableSet<Screen> remoteFramebufferLayout) => _connection.RemoteFramebufferLayout = remoteFramebufferLayout;

            /// <summary>
            /// Sets the value of the <seealso cref="RfbConnection.DesktopName"/> property on the <see cref="RfbConnection"/> object.
            /// </summary>
            /// <param name="desktopName">The new desktop name.</param>
            public void SetDesktopName(string? desktopName) => _connection.DesktopName = desktopName;

            /// <summary>
            /// Sets the value of the <seealso cref="RfbConnection.ContinuousUpdatesEnabled"/> property on the <see cref="RfbConnection"/> object.
            /// </summary>
            /// <param name="continuousUpdatesEnabled">The new state.</param>
            public void SetContinuousUpdatesEnabled(bool continuousUpdatesEnabled) => _connection.ContinuousUpdatesEnabled = continuousUpdatesEnabled;
        }
    }
}
