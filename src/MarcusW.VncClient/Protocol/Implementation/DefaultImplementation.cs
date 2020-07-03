using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MarcusW.VncClient.Protocol.Encodings;
using MarcusW.VncClient.Protocol.Implementation.SecurityTypes;
using MarcusW.VncClient.Protocol.Implementation.Services.Communication;
using MarcusW.VncClient.Protocol.Implementation.Services.Handshaking;
using MarcusW.VncClient.Protocol.Implementation.Services.Initialization;
using MarcusW.VncClient.Protocol.Implementation.Services.Transports;
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
    /// Represents the method that builds a new <see cref="IEncoding"/> collection.
    /// </summary>
    /// <param name="context">Details about the associated connection.</param>
    public delegate IEnumerable<IEncoding> EncodingsCollectionBuilderDelegate(RfbConnectionContext context);

    /// <summary>
    /// Default implementation of the RFB protocol.
    /// </summary>
    public class DefaultImplementation : IRfbProtocolImplementation
    {
        private readonly SecurityTypesCollectionBuilderDelegate _securityTypesCollectionBuilder;
        private readonly EncodingsCollectionBuilderDelegate _encodingsCollectionBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultImplementation"/>.
        /// </summary>
        public DefaultImplementation() : this(GetDefaultSecurityTypes, GetDefaultEncodings) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultImplementation"/>.
        /// </summary>
        /// <param name="securityTypesCollectionBuilder">A method that newly creates all the security types that are supported by this protocol instance. See <see cref="GetDefaultSecurityTypes"/>.</param>
        /// <param name="encodingsCollectionBuilder">A method that newly creates all the encodings that are supported by this protocol instance. See <see cref="GetDefaultEncodings"/>.</param>
        public DefaultImplementation(SecurityTypesCollectionBuilderDelegate securityTypesCollectionBuilder, EncodingsCollectionBuilderDelegate encodingsCollectionBuilder)
        {
            _securityTypesCollectionBuilder = securityTypesCollectionBuilder ?? throw new ArgumentNullException(nameof(securityTypesCollectionBuilder));
            _encodingsCollectionBuilder = encodingsCollectionBuilder ?? throw new ArgumentNullException(nameof(encodingsCollectionBuilder));
        }

        /// <inhertitdoc />
        public IImmutableDictionary<byte, ISecurityType> CreateSecurityTypesCollection(RfbConnectionContext context)
            => BuildImmutableDictionary(_securityTypesCollectionBuilder.Invoke(context), v => v.Id);

        /// <inhertitdoc />
        public IImmutableDictionary<int, IEncoding> CreateEncodingsCollection(RfbConnectionContext context)
            => BuildImmutableDictionary(_encodingsCollectionBuilder.Invoke(context), v => v.Id);

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

            yield return new NoSecurityType();
            yield return new VncAuthenticationSecurityType(context);
        }

        /// <summary>
        /// Builds a collection with all RFB encodings that are officially supported by this protocol implementation.
        /// Feel free to extend the returned enumerable with custom encodings.
        /// </summary>
        /// <param name="context">The connection context.</param>
        /// <returns>The encoding collection.</returns>
        public static IEnumerable<IEncoding> GetDefaultEncodings(RfbConnectionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // TODO: Add encodings
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
