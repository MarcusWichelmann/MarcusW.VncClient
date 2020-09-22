using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Frame;
using MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Incoming;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;
using MarcusW.VncClient.Protocol.Implementation.SecurityTypes;
using MarcusW.VncClient.Protocol.Implementation.Services.Communication;
using MarcusW.VncClient.Protocol.Implementation.Services.Handshaking;
using MarcusW.VncClient.Protocol.Implementation.Services.Initialization;
using MarcusW.VncClient.Protocol.Implementation.Services.Transports;
using MarcusW.VncClient.Protocol.MessageTypes;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Protocol.Services;

namespace MarcusW.VncClient.Protocol.Implementation
{
    /// <summary>
    /// Represents the method that builds a new <see cref="ISecurityType"/> collection.
    /// </summary>
    /// <param name="context">Details about the associated connection.</param>
    public delegate IEnumerable<ISecurityType> SecurityTypesCollectionBuilderDelegate(RfbConnectionContext context);

    /// <summary>
    /// Represents the method that builds a new <see cref="IMessageType"/> collection.
    /// </summary>
    /// <param name="context">Details about the associated connection.</param>
    public delegate IEnumerable<IMessageType> MessagesCollectionBuilderDelegate(RfbConnectionContext context);

    /// <summary>
    /// Represents the method that builds a new <see cref="IEncodingType"/> collection.
    /// </summary>
    /// <param name="context">Details about the associated connection.</param>
    public delegate IEnumerable<IEncodingType> EncodingTypesCollectionBuilderDelegate(RfbConnectionContext context);

    /// <summary>
    /// Default implementation of the RFB protocol.
    /// </summary>
    public class DefaultImplementation : IRfbProtocolImplementation
    {
        private readonly SecurityTypesCollectionBuilderDelegate _securityTypesCollectionBuilder;
        private readonly MessagesCollectionBuilderDelegate _messagesCollectionBuilder;
        private readonly EncodingTypesCollectionBuilderDelegate _encodingTypesCollectionBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultImplementation"/>.
        /// </summary>
        public DefaultImplementation() : this(GetDefaultSecurityTypes, GetDefaultMessageTypes, GetDefaultEncodingTypes) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultImplementation"/>.
        /// </summary>
        /// <param name="securityTypesCollectionBuilder">A method that newly creates all the security types that are supported by this protocol instance. See <see cref="GetDefaultSecurityTypes"/>.</param>
        /// <param name="messagesCollectionBuilder">A method that newly creates all the messages that are supported by this protocol instance. See <see cref="GetDefaultMessageTypes"/>.</param>
        /// <param name="encodingTypesCollectionBuilder">A method that newly creates all the encoding types that are supported by this protocol instance. See <see cref="GetDefaultEncodingTypes"/>.</param>
        public DefaultImplementation(SecurityTypesCollectionBuilderDelegate securityTypesCollectionBuilder, MessagesCollectionBuilderDelegate messagesCollectionBuilder,
            EncodingTypesCollectionBuilderDelegate encodingTypesCollectionBuilder)
        {
            _securityTypesCollectionBuilder = securityTypesCollectionBuilder ?? throw new ArgumentNullException(nameof(securityTypesCollectionBuilder));
            _messagesCollectionBuilder = messagesCollectionBuilder ?? throw new ArgumentNullException(nameof(messagesCollectionBuilder));
            _encodingTypesCollectionBuilder = encodingTypesCollectionBuilder ?? throw new ArgumentNullException(nameof(encodingTypesCollectionBuilder));
        }

        /// <inheritdoc />
        public virtual IRfbProtocolState CreateStateObject(RfbConnectionContext context) => new ProtocolState(context);

        /// <inhertitdoc />
        public virtual IImmutableSet<ISecurityType> CreateSecurityTypesCollection(RfbConnectionContext context)
            => _securityTypesCollectionBuilder.Invoke(context).ToImmutableHashSet();

        /// <inhertitdoc />
        public virtual IImmutableSet<IMessageType> CreateMessageTypesCollection(RfbConnectionContext context) => _messagesCollectionBuilder.Invoke(context).ToImmutableHashSet();

        /// <inhertitdoc />
        public virtual IImmutableSet<IEncodingType> CreateEncodingTypesCollection(RfbConnectionContext context)
            => _encodingTypesCollectionBuilder.Invoke(context).ToImmutableHashSet();

        /// <inheritdoc />
        public virtual ITransportConnector CreateTransportConnector(RfbConnectionContext context) => new TransportConnector(context);

        /// <inheritdoc />
        public virtual IRfbHandshaker CreateRfbHandshaker(RfbConnectionContext context) => new RfbHandshaker(context);

        /// <inheritdoc />
        public virtual IRfbInitializer CreateRfbInitializer(RfbConnectionContext context) => new RfbInitializer(context);

        /// <inheritdoc />
        public virtual IRfbMessageReceiver CreateMessageReceiver(RfbConnectionContext context) => new RfbMessageReceiver(context);

        /// <inheritdoc />
        public virtual IRfbMessageSender CreateMessageSender(RfbConnectionContext context) => new RfbMessageSender(context);

        /// <inheritdoc />
        public IZLibInflater CreateZLibInflater(RfbConnectionContext context) => new ZLibInflater();

        /// <summary>
        /// Builds a collection with all RFB security types that are officially supported by this protocol implementation.
        /// Feel free to extend the returned enumerable with custom types.
        /// </summary>
        /// <param name="context">The connection context.</param>
        /// <returns>The security type collection.</returns>
        public static IEnumerable<ISecurityType> GetDefaultSecurityTypes(RfbConnectionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            yield return new NoneSecurityType(context);
            yield return new VncAuthenticationSecurityType(context);
        }

        /// <summary>
        /// Builds a collection with all RFB message types that are officially supported by this protocol implementation.
        /// Feel free to extend the returned enumerable with custom message types.
        /// </summary>
        /// <param name="context">The connection context.</param>
        /// <returns>The message types collection.</returns>
        public static IEnumerable<IMessageType> GetDefaultMessageTypes(RfbConnectionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Incoming
            yield return new FramebufferUpdateMessageType(context);
            yield return new SetColourMapEntriesMessageType(context);
            yield return new BellMessageType(context);
            yield return new ServerCutTextMessageType(context);
            yield return new ServerFenceMessageType(context);
            yield return new EndOfContinuousUpdatesMessageType(context);

            // Outgoing
            yield return new SetEncodingsMessageType();
            yield return new FramebufferUpdateRequestMessageType();
            yield return new ClientFenceMessageType();
            yield return new EnableContinuousUpdatesMessageType();
            yield return new PointerEventMessageType();
            yield return new KeyEventMessageType();
        }

        /// <summary>
        /// Builds a collection with all RFB encoding types that are officially supported by this protocol implementation.
        /// Feel free to extend the returned enumerable with custom types.
        /// </summary>
        /// <param name="context">The connection context.</param>
        /// <returns>The encoding types collection.</returns>
        public static IEnumerable<IEncodingType> GetDefaultEncodingTypes(RfbConnectionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Frame
            yield return new RawEncodingType();
            yield return new ZLibEncodingType(context);
            yield return new ZrleEncodingType(context);

            // Pseudo
            yield return new FenceEncodingType();
            yield return new ContinuousUpdatesEncodingType();
            yield return new LastRectEncodingType();
        }
    }
}
