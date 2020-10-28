using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Threading;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing
{
    /// <summary>
    /// A message type for sending <see cref="PointerEventMessage"/>s.
    /// </summary>
    public class PointerEventMessageType : IOutgoingMessageType
    {
        /// <inheritdoc />
        public byte Id => (byte)WellKnownOutgoingMessageType.PointerEvent;

        /// <inheritdoc />
        public string Name => "PointerEvent";

        /// <inheritdoc />
        public bool IsStandardMessageType => true;

        /// <inheritdoc />
        public void WriteToTransport(IOutgoingMessage<IOutgoingMessageType> message, ITransport transport, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (!(message is PointerEventMessage pointerEventMessage))
                throw new ArgumentException($"Message is no {nameof(PointerEventMessage)}.", nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            // Get pointer position
            var posX = (ushort)Math.Max(0, pointerEventMessage.PointerPosition.X);
            var posY = (ushort)Math.Max(0, pointerEventMessage.PointerPosition.Y);

            Span<byte> buffer = stackalloc byte[6];

            // Message type
            buffer[0] = Id;

            // Pressed buttons mask
            buffer[1] = (byte)pointerEventMessage.PressedButtons;

            // Pointer position
            BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], posX);
            BinaryPrimitives.WriteUInt16BigEndian(buffer[4..], posY);

            // Write message to stream
            transport.Stream.Write(buffer);
        }
    }

    /// <summary>
    /// A message for telling the server about mouse pointer events.
    /// </summary>
    public class PointerEventMessage : IOutgoingMessage<PointerEventMessageType>
    {
        /// <summary>
        /// Gets the pointer position.
        /// </summary>
        public Position PointerPosition { get; }

        /// <summary>
        /// Gets the pressed buttons.
        /// </summary>
        public MouseButtons PressedButtons { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointerEventMessage"/>.
        /// </summary>
        /// <param name="pointerPosition">The pointer position.</param>
        /// <param name="pressedButtons">The pressed buttons.</param>
        public PointerEventMessage(Position pointerPosition, MouseButtons pressedButtons)
        {
            PointerPosition = pointerPosition;
            PressedButtons = pressedButtons;
        }

        /// <inheritdoc />
        public string? GetParametersOverview() => $"PointerPosition: {PointerPosition}, PressedButtons: {PressedButtons}";
    }
}
