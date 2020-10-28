using System;
using System.Buffers.Binary;
using System.Threading;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing
{
    /// <summary>
    /// A message type for sending <see cref="KeyEventMessage"/>s.
    /// </summary>
    public class KeyEventMessageType : IOutgoingMessageType
    {
        /// <inheritdoc />
        public byte Id => (byte)WellKnownOutgoingMessageType.KeyEvent;

        /// <inheritdoc />
        public string Name => "KeyEvent";

        /// <inheritdoc />
        public bool IsStandardMessageType => true;

        /// <inheritdoc />
        public void WriteToTransport(IOutgoingMessage<IOutgoingMessageType> message, ITransport transport, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (!(message is KeyEventMessage keyEventMessage))
                throw new ArgumentException($"Message is no {nameof(KeyEventMessage)}.", nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            Span<byte> buffer = stackalloc byte[8];

            // Message type
            buffer[0] = Id;

            // Down flag (followed by two bytes padding)
            buffer[1] = (byte)(keyEventMessage.DownFlag ? 1 : 0);

            // Keysym
            BinaryPrimitives.WriteUInt32BigEndian(buffer[4..], (uint)keyEventMessage.KeySymbol);

            // Write message to stream
            transport.Stream.Write(buffer);
        }
    }

    /// <summary>
    /// A message for telling the server about key events.
    /// </summary>
    public class KeyEventMessage : IOutgoingMessage<KeyEventMessageType>
    {
        /// <summary>
        /// Gets whether this is a key down or key up event.
        /// </summary>
        public bool DownFlag { get; }

        /// <summary>
        /// Gets the key symbol as defined by the X Window System.
        /// </summary>
        public KeySymbol KeySymbol { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyEventMessage"/>.
        /// </summary>
        /// <param name="downFlag">True, if this is a key down event, false for key up.</param>
        /// <param name="keySymbol">The key symbol as defined by the X Window System.</param>
        public KeyEventMessage(bool downFlag, KeySymbol keySymbol)
        {
            DownFlag = downFlag;
            KeySymbol = keySymbol;
        }

        /// <inheritdoc />
        public string? GetParametersOverview() => $"DownFlag: {DownFlag}, KeySymbol: {KeySymbol}";
    }
}
