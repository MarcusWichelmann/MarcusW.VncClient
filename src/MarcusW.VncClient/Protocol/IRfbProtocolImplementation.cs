using System.Collections.Immutable;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.MessageTypes;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Protocol.Services;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// Provides access to different elements of a RFB protocol implementation.
    /// </summary>
    public interface IRfbProtocolImplementation
    {
        /// <summary>
        /// Creates a new <see cref="IRfbProtocolState"/> state object.
        /// </summary>
        /// <param name="context">Details about the associated connection.</param>
        /// <returns>A new state object.</returns>
        IRfbProtocolState CreateStateObject(RfbConnectionContext context);

        /// <summary>
        /// Creates a new collection of all supported <see cref="ISecurityType"/>s.
        /// </summary>
        /// <remarks>
        /// Make sure the security types are newly instantiated on each call, because it's
        /// not guaranteed that they can safely be used across multiple connections simultaneously.
        /// </remarks>
        /// <param name="context">Details about the associated connection.</param>
        /// <returns>An immutable set with all supported security types.</returns>
        IImmutableSet<ISecurityType> CreateSecurityTypesCollection(RfbConnectionContext context);

        /// <summary>
        /// Creates a new collection of all supported <see cref="IMessageType"/>s.
        /// </summary>
        /// <remarks>
        /// Make sure the message types are newly instantiated on each call, because it's
        /// not guaranteed that they can safely be used across multiple connections simultaneously.
        /// </remarks>
        /// <param name="context">Details about the associated connection.</param>
        /// <returns>An immutable set with all supported message types.</returns>
        IImmutableSet<IMessageType> CreateMessageTypesCollection(RfbConnectionContext context);

        /// <summary>
        /// Creates a new collection of all supported <see cref="IEncodingType"/>s.
        /// </summary>
        /// <remarks>
        /// Make sure the encoding types are newly instantiated on each call, because it's
        /// not guaranteed that they can safely be used across multiple connections simultaneously.
        /// </remarks>
        /// <param name="context">Details about the associated connection.</param>
        /// <returns>An immutable set with all supported encoding types.</returns>
        IImmutableSet<IEncodingType> CreateEncodingTypesCollection(RfbConnectionContext context);

        /// <summary>
        /// Creates a new <see cref="ITransportConnector"/>.
        /// </summary>
        /// <param name="context">Details about the associated connection.</param>
        /// <returns>A new instance of the transport connector.</returns>
        ITransportConnector CreateTransportConnector(RfbConnectionContext context);

        /// <summary>
        /// Creates a new <see cref="IRfbHandshaker"/>.
        /// </summary>
        /// <param name="context">Details about the associated connection.</param>
        /// <returns>A new instance of the RFB handshaker.</returns>
        IRfbHandshaker CreateRfbHandshaker(RfbConnectionContext context);

        /// <summary>
        /// Creates a new <see cref="IRfbInitializer"/>.
        /// </summary>
        /// <param name="context">Details about the associated connection.</param>
        /// <returns>A new instance of the RFB initializer.</returns>
        IRfbInitializer CreateRfbInitializer(RfbConnectionContext context);

        /// <summary>
        /// Creates a new <see cref="IRfbMessageReceiver"/>.
        /// </summary>
        /// <param name="context">Details about the associated connection.</param>
        /// <returns>A new instance of the message receiver.</returns>
        IRfbMessageReceiver CreateMessageReceiver(RfbConnectionContext context);

        /// <summary>
        /// Creates a new <see cref="IRfbMessageSender"/>.
        /// </summary>
        /// <param name="context">Details about the associated connection.</param>
        /// <returns>A new instance of the message sender.</returns>
        IRfbMessageSender CreateMessageSender(RfbConnectionContext context);
    }
}
