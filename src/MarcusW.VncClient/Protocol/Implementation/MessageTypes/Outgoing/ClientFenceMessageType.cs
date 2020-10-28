using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Threading;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing
{
    /// <summary>
    /// A message type for sending <see cref="ClientFenceMessage"/>s.
    /// </summary>
    public class ClientFenceMessageType : IOutgoingMessageType
    {
        /// <inheritdoc />
        public byte Id => (byte)WellKnownOutgoingMessageType.ClientFence;

        /// <inheritdoc />
        public string Name => "ClientFence";

        /// <inheritdoc />
        public bool IsStandardMessageType => false;

        /// <inheritdoc />
        public void WriteToTransport(IOutgoingMessage<IOutgoingMessageType> message, ITransport transport, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (!(message is ClientFenceMessage clientFenceMessage))
                throw new ArgumentException($"Message is no {nameof(ClientFenceMessage)}.", nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            // Message size
            int messageSize = 1 + 3 + sizeof(uint) + sizeof(byte) + clientFenceMessage.Payload.Length;

            // Allocate buffer
            Debug.Assert(messageSize <= 1024, "messageSize <= 1024");
            Span<byte> buffer = stackalloc byte[messageSize];

            // Message header
            buffer[0] = Id;

            // Padding
            buffer[1] = 0;
            buffer[2] = 0;
            buffer[3] = 0;

            // Flags
            BinaryPrimitives.WriteUInt32BigEndian(buffer[4..], (uint)clientFenceMessage.Flags);

            // Payload length
            buffer[8] = (byte)clientFenceMessage.Payload.Length;

            // Copy payload
            clientFenceMessage.Payload.CopyTo(buffer[9..]);

            // Write buffer to stream
            transport.Stream.Write(buffer);
        }
    }

    /// <summary>
    /// A message for requesting a ServerFence or replying to one.
    /// </summary>
    public class ClientFenceMessage : IOutgoingMessage<ClientFenceMessageType>
    {
        /// <summary>
        /// Gets the fence flags.
        /// </summary>
        public FenceFlags Flags { get; }

        /// <summary>
        /// Gets the fence payload.
        /// </summary>
        public byte[] Payload { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientFenceMessage"/>.
        /// </summary>
        /// <param name="flags">The fence flags.</param>
        /// <param name="payload">The fence payload (limited to 64 bytes).</param>
        public ClientFenceMessage(FenceFlags flags, byte[] payload)
        {
            Flags = flags; // Enum is not checked to allow extended flag bits.
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));

            if (payload.Length > 64)
                throw new ArgumentException("Payload length must not exceed 64 bytes.", nameof(payload));
        }

        /// <inheritdoc />
        public string? GetParametersOverview() => $"Flags: {Flags}, Payload: {BitConverter.ToString(Payload)}";
    }
}
