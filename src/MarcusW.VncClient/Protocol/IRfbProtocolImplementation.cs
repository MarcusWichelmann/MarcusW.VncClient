using System.Collections.Generic;
using System.Collections.Immutable;
using MarcusW.VncClient.Protocol.Encodings;
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
        /// Creates a new collection of all supported <see cref="ISecurityType"/>s.
        /// </summary>
        /// <remarks>
        /// Make sure the security types are newly instantiated on each call, because it's
        /// not guaranteed that they can safely be used across multiple connections simultaneously.
        /// </remarks>
        /// <param name="context">Details about the associated connection.</param>
        /// <returns>An immutable dictionary that maps security type ids to security types.</returns>
        IImmutableDictionary<byte, ISecurityType> CreateSecurityTypesCollection(RfbConnectionContext context);

        /// <summary>
        /// Creates a new collection of all supported <see cref="IEncoding"/>s.
        /// </summary>
        /// <remarks>
        /// Make sure the encodings are newly instantiated on each call, because it's
        /// not guaranteed that they can safely be used across multiple connections simultaneously.
        /// </remarks>
        /// <param name="context">Details about the associated connection.</param>
        /// <returns>An immutable dictionary that maps encoding ids to encodings.</returns>
        IImmutableDictionary<int, IEncoding> CreateEncodingsCollection(RfbConnectionContext context);

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
