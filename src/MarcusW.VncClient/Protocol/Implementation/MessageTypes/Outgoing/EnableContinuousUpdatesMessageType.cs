using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Threading;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing
{
    /// <summary>
    /// A message type for sending <see cref="EnableContinuousUpdatesMessage"/>s.
    /// </summary>
    public class EnableContinuousUpdatesMessageType : IOutgoingMessageType
    {
        /// <inheritdoc />
        public byte Id => (byte)WellKnownOutgoingMessageType.EnableContinuousUpdates;

        /// <inheritdoc />
        public string Name => "EnableContinuousUpdates";

        /// <inheritdoc />
        public bool IsStandardMessageType => false;

        /// <inheritdoc />
        public void WriteToTransport(IOutgoingMessage<IOutgoingMessageType> message, ITransport transport, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (!(message is EnableContinuousUpdatesMessage enableContinuousUpdatesMessage))
                throw new ArgumentException($"Message is no {nameof(EnableContinuousUpdatesMessage)}.", nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            // Message size
            const int messageSize = 1 + sizeof(byte) + 4 * sizeof(ushort);

            // Allocate buffer
            Debug.Assert(messageSize <= 1024, "messageSize <= 1024");
            Span<byte> buffer = stackalloc byte[messageSize];

            // Message header
            buffer[0] = Id;

            // Enable?
            buffer[1] = (byte)(enableContinuousUpdatesMessage.Enable ? 1 : 0);

            // Rectangle
            Rectangle rectangle = enableContinuousUpdatesMessage.Rectangle;
            BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], (ushort)rectangle.Position.X);
            BinaryPrimitives.WriteUInt16BigEndian(buffer[4..], (ushort)rectangle.Position.Y);
            BinaryPrimitives.WriteUInt16BigEndian(buffer[6..], (ushort)rectangle.Size.Width);
            BinaryPrimitives.WriteUInt16BigEndian(buffer[8..], (ushort)rectangle.Size.Height);

            // Write buffer to stream
            transport.Stream.Write(buffer);
        }
    }

    /// <summary>
    /// A message for enabling continuous updates.
    /// </summary>
    public class EnableContinuousUpdatesMessage : IOutgoingMessage<EnableContinuousUpdatesMessageType>
    {
        /// <summary>
        /// Gets whether continuous updates should be enabled or disabled.
        /// </summary>
        public bool Enable { get; }

        /// <summary>
        /// Gets the rectangle that should be continuously updated.
        /// </summary>
        public Rectangle Rectangle { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableContinuousUpdatesMessage"/>.
        /// </summary>
        /// <param name="enable">True if continuous updates should be enabled, false to disable them.</param>
        /// <param name="rectangle">The rectangle that should be continuously updated.</param>
        public EnableContinuousUpdatesMessage(bool enable, Rectangle rectangle)
        {
            Enable = enable;
            Rectangle = rectangle;
        }

        /// <inheritdoc />
        public string? GetParametersOverview() => $"Enable: {Enable}, Rectangle: {Rectangle}";
    }
}
