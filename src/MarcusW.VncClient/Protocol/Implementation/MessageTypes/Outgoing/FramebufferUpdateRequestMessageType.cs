using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Threading;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Incoming;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing
{
    /// <summary>
    /// A message type for sending <see cref="FramebufferUpdateRequestMessage"/>s.
    /// </summary>
    public class FramebufferUpdateRequestMessageType : IOutgoingMessageType
    {
        /// <inheritdoc />
        public byte Id => (byte)WellKnownOutgoingMessageType.FramebufferUpdateRequest;

        /// <inheritdoc />
        public string Name => "FramebufferUpdateRequest";

        /// <inheritdoc />
        public bool IsStandardMessageType => true;

        /// <inheritdoc />
        public void WriteToTransport(IOutgoingMessage<IOutgoingMessageType> message, ITransport transport, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (!(message is FramebufferUpdateRequestMessage framebufferUpdateRequestMessage))
                throw new ArgumentException($"Message is no {nameof(FramebufferUpdateRequestMessage)}.", nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            // Message size
            const int messageSize = 1 + sizeof(byte) + 4 * sizeof(ushort);

            // Allocate buffer
            Debug.Assert(messageSize <= 1024, "messageSize <= 1024");
            Span<byte> buffer = stackalloc byte[messageSize];

            // Message header
            buffer[0] = Id;

            // Incremental?
            buffer[1] = (byte)(framebufferUpdateRequestMessage.Incremental ? 1 : 0);

            // Rectangle
            Rectangle rectangle = framebufferUpdateRequestMessage.Rectangle;
            BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], (ushort)rectangle.Position.X);
            BinaryPrimitives.WriteUInt16BigEndian(buffer[4..], (ushort)rectangle.Position.Y);
            BinaryPrimitives.WriteUInt16BigEndian(buffer[6..], (ushort)rectangle.Size.Width);
            BinaryPrimitives.WriteUInt16BigEndian(buffer[8..], (ushort)rectangle.Size.Height);

            // Write buffer to stream
            transport.Stream.Write(buffer);
        }
    }

    /// <summary>
    /// A message that requests a framebuffer update from the server.
    /// </summary>
    public class FramebufferUpdateRequestMessage : IOutgoingMessage<FramebufferUpdateRequestMessageType>
    {
        /// <summary>
        /// Gets whether the framebuffer update should be incremental.
        /// </summary>
        public bool Incremental { get; }

        /// <summary>
        /// Gets the rectangle that should be updated.
        /// </summary>
        public Rectangle Rectangle { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FramebufferUpdateRequestMessage"/>.
        /// </summary>
        /// <param name="incremental">True if the framebuffer update should be incremental, otherwise false.</param>
        /// <param name="rectangle">The rectangle that should be updated.</param>
        public FramebufferUpdateRequestMessage(bool incremental, Rectangle rectangle)
        {
            Incremental = incremental;
            Rectangle = rectangle;
        }
    }
}
