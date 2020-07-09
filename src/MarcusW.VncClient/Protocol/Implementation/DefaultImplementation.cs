using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MarcusW.VncClient.Protocol.EncodingTypes;
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
        public DefaultImplementation() : this(GetDefaultSecurityTypes, GetDefaultMessages, GetDefaultEncodingTypes) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultImplementation"/>.
        /// </summary>
        /// <param name="securityTypesCollectionBuilder">A method that newly creates all the security types that are supported by this protocol instance. See <see cref="GetDefaultSecurityTypes"/>.</param>
        /// <param name="messagesCollectionBuilder">A method that newly creates all the messages that are supported by this protocol instance. See <see cref="GetDefaultMessages"/>.</param>
        /// <param name="encodingTypesCollectionBuilder">A method that newly creates all the encoding types that are supported by this protocol instance. See <see cref="GetDefaultEncodingTypes"/>.</param>
        public DefaultImplementation(SecurityTypesCollectionBuilderDelegate securityTypesCollectionBuilder, MessagesCollectionBuilderDelegate messagesCollectionBuilder,
            EncodingTypesCollectionBuilderDelegate encodingTypesCollectionBuilder)
        {
            _securityTypesCollectionBuilder = securityTypesCollectionBuilder ?? throw new ArgumentNullException(nameof(securityTypesCollectionBuilder));
            _messagesCollectionBuilder = messagesCollectionBuilder ?? throw new ArgumentNullException(nameof(messagesCollectionBuilder));
            _encodingTypesCollectionBuilder = encodingTypesCollectionBuilder ?? throw new ArgumentNullException(nameof(encodingTypesCollectionBuilder));
        }

        /// <inheritdoc />
        public IRfbProtocolState CreateStateObject(RfbConnectionContext context) => new ProtocolState(context);

        /// <inhertitdoc />
        public IImmutableDictionary<byte, ISecurityType> CreateSecurityTypesCollection(RfbConnectionContext context)
            => BuildImmutableDictionary(_securityTypesCollectionBuilder.Invoke(context), v => v.Id);

        /// <inhertitdoc />
        public IImmutableDictionary<byte, IMessageType> CreateMessageTypesCollection(RfbConnectionContext context)
            => BuildImmutableDictionary(_messagesCollectionBuilder.Invoke(context), v => v.Id);

        /// <inhertitdoc />
        public IImmutableDictionary<int, IEncodingType> CreateEncodingTypesCollection(RfbConnectionContext context)
            => BuildImmutableDictionary(_encodingTypesCollectionBuilder.Invoke(context), v => v.Id);

        /// <inheritdoc />
        public virtual ITransportConnector CreateTransportConnector(RfbConnectionContext context) => new TransportConnector(context);

        /// <inheritdoc />
        public virtual IRfbHandshaker CreateRfbHandshaker(RfbConnectionContext context) => new RfbHandshaker(context);

        /// <inheritdoc />
        public IRfbInitializer CreateRfbInitializer(RfbConnectionContext context) => new RfbInitializer(context);

        /// <inheritdoc />
        public virtual IRfbMessageReceiver CreateMessageReceiver(RfbConnectionContext context) => new RfbMessageReceiver(context);

        /// <inheritdoc />
        public virtual IRfbMessageSender CreateMessageSender(RfbConnectionContext context) => new RfbMessageSender(context);

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

            yield return new NoSecurityType(context);
            yield return new VncAuthenticationSecurityType(context);
        }

        /// <summary>
        /// Builds a collection with all RFB messages that are officially supported by this protocol implementation.
        /// Feel free to extend the returned enumerable with custom messages.
        /// </summary>
        /// <param name="context">The connection context.</param>
        /// <returns>The message collection.</returns>
        public static IEnumerable<IMessageType> GetDefaultMessages(RfbConnectionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // TODO: Add messages
            yield break;
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

            // TODO: Add encoding types
            yield break;
        }

        // Helper function to extract the keys from a list of values and create an immutable key->value map from them.
        private static IImmutableDictionary<TKey, TValue> BuildImmutableDictionary<TKey, TValue>(IEnumerable<TValue> values, Func<TValue, TKey> keySelector)
        {
            var builder = ImmutableDictionary.CreateBuilder<TKey, TValue>();
            builder.AddRange(values.Select(value => new KeyValuePair<TKey, TValue>(keySelector(value), value)));
            return builder.ToImmutable();
        }
    }
}