using System.Threading;

namespace MarcusW.VncClient.Protocol.MessageTypes
{
    /// <summary>
    /// Represents a client-to-server message type of the RFB protocol.
    /// </summary>
    public interface IOutgoingMessageType : IMessageType
    {
        /// <summary>
        /// Writes the <see cref="message"/> to the transport stream.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="transport">The target transport.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void WriteToTransport(IOutgoingMessage<IOutgoingMessageType> message, ITransport transport, CancellationToken cancellationToken);
    }
}
