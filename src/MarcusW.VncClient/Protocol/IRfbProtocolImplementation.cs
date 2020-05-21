using System.Collections.Generic;
using MarcusW.VncClient.Protocol.Encodings;
using MarcusW.VncClient.Protocol.Services.Communication;
using MarcusW.VncClient.Protocol.Services.Connection;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// Provides access to different elements of a RFB protocol implementation.
    /// </summary>
    public interface IRfbProtocolImplementation
    {
        /// <summary>
        /// Gets the supported encodings.
        /// </summary>
        IReadOnlyCollection<IEncoding> SupportedEncodings { get; }

        /// <summary>
        /// Creates a new <see cref="ITcpConnector"/>.
        /// </summary>
        /// <returns>A new instance of the TCP connector.</returns>
        ITcpConnector CreateTcpConnector();

        /// <summary>
        /// Creates a new <see cref="IRfbMessageReceiver"/>.
        /// </summary>
        /// <param name="connection">The associated connection.</param>
        /// <returns>A new instance of the message receiver.</returns>
        IRfbMessageReceiver CreateMessageReceiver(RfbConnection connection);

        /// <summary>
        /// Creates a new <see cref="IRfbMessageSender"/>.
        /// </summary>
        /// <param name="connection">The associated connection.</param>
        /// <returns>A new instance of the message sender.</returns>
        IRfbMessageSender CreateMessageSender(RfbConnection connection);
    }
}
