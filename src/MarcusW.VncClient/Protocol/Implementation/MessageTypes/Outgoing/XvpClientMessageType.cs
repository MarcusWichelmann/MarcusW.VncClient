using System;
using System.Threading;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing
{
    /// <summary>
    /// Defines the xvp operation codes that can be sent to the server.
    /// </summary>
    public enum XvpOperation : byte
    {
        /// <summary>
        /// Request a clean shutdown of the remote system.
        /// </summary>
        Shutdown = 2,

        /// <summary>
        /// Request a clean reboot of the remote system.
        /// </summary>
        Reboot = 3,

        /// <summary>
        /// Request an abrupt reset of the remote system.
        /// </summary>
        Reset = 4
    }

    /// <summary>
    /// A message type for sending xvp client messages (VM power control commands).
    /// </summary>
    public class XvpClientMessageType : IOutgoingMessageType
    {
        /// <summary>
        /// The current xvp extension version.
        /// </summary>
        public const byte XvpVersion = 1;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownOutgoingMessageType.XvpClient;

        /// <inheritdoc />
        public string Name => "XvpClient";

        /// <inheritdoc />
        public bool IsStandardMessageType => false;

        /// <inheritdoc />
        public void WriteToTransport(IOutgoingMessage<IOutgoingMessageType> message, ITransport transport, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (message is not XvpClientMessage xvpMessage)
                throw new ArgumentException($"Message is not a {nameof(XvpClientMessage)}.", nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            // Message format:
            // 1 byte - message-type (250)
            // 1 byte - padding
            // 1 byte - xvp-extension-version
            // 1 byte - xvp-message-code
            Span<byte> buffer = stackalloc byte[4];
            buffer[0] = Id;
            buffer[1] = 0; // padding
            buffer[2] = XvpVersion;
            buffer[3] = (byte)xvpMessage.Operation;

            transport.Stream.Write(buffer);
        }
    }

    /// <summary>
    /// A message for requesting an xvp operation (shutdown, reboot, or reset).
    /// </summary>
    public class XvpClientMessage : IOutgoingMessage<XvpClientMessageType>
    {
        /// <summary>
        /// Gets the xvp operation to request.
        /// </summary>
        public XvpOperation Operation { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XvpClientMessage"/>.
        /// </summary>
        /// <param name="operation">The xvp operation to request.</param>
        public XvpClientMessage(XvpOperation operation)
        {
            if (!Enum.IsDefined(typeof(XvpOperation), operation))
                throw new ArgumentException("Invalid xvp operation.", nameof(operation));

            Operation = operation;
        }

        /// <inheritdoc />
        public string? GetParametersOverview() => $"Operation: {Operation}";
    }
}
