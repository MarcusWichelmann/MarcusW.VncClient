using System.Threading;

namespace MarcusW.VncClient.Protocol.MessageTypes
{
    /// <summary>
    /// Represents a server-to-client message type of the RFB protocol.
    /// </summary>
    public interface IIncomingMessageType : IMessageType
    {
        /// <summary>
        /// Reads the message (everything after the message type byte) from the transport stream and processes it.
        /// </summary>
        /// <param name="transport">The transport to read from.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void ReadMessage(ITransport transport, CancellationToken cancellationToken = default);
    }
}
