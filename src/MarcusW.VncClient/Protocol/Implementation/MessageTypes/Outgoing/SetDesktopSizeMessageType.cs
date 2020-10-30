using System.Threading;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing
{
    /// <summary>
    /// A message type for sending <see cref="SetDesktopSizeMessage"/>s.
    /// </summary>
    public class SetDesktopSizeMessageType : IOutgoingMessageType
    {
        /// <inheritdoc />
        public byte Id => (byte)WellKnownOutgoingMessageType.SetDesktopSize;

        /// <inheritdoc />
        public string Name => "SetDesktopSize";

        /// <inheritdoc />
        public bool IsStandardMessageType => false;

        /// <inheritdoc />
        public void WriteToTransport(IOutgoingMessage<IOutgoingMessageType> message, ITransport transport, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }

    public class SetDesktopSizeMessage : IOutgoingMessage<SetDesktopSizeMessageType>
    {
        // TODO

        /// <inheritdoc />
        public string? GetParametersOverview() => "TODO TODO TODO TODO TODO TODO";
    }
}
