using System;
using System.Buffers.Binary;
using System.Text;
using System.Threading;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing
{
    /// <summary>
    /// A message type for sending <see cref="ClientCutTextMessage"/>s.
    /// </summary>
    public class ClientCutTextMessageType : IOutgoingMessageType
    {
        /// <inheritdoc />
        public byte Id => (byte)WellKnownOutgoingMessageType.ClientCutText;

        /// <inheritdoc />
        public string Name => "ClientCutText";

        /// <inheritdoc />
        public bool IsStandardMessageType => true;

        /// <inheritdoc />
        public void WriteToTransport(IOutgoingMessage<IOutgoingMessageType> message, ITransport transport, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (!(message is ClientCutTextMessage clientCutTextMessage))
                throw new ArgumentException($"Message is no {nameof(ClientCutTextMessage)}.", nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            // Encode text using ISO-8859-1 (Latin1) as per VNC protocol specification
            Encoding latin1Encoding = Encoding.GetEncoding("ISO-8859-1");
            byte[] textBytes = latin1Encoding.GetBytes(clientCutTextMessage.Text ?? string.Empty);
            uint textLength = (uint)textBytes.Length;

            // Allocate buffer: 1 byte message type + 3 bytes padding + 4 bytes length = 8 bytes header
            Span<byte> header = stackalloc byte[8];

            // Message type
            header[0] = Id;

            // Padding (3 bytes, set to 0)
            header[1] = 0;
            header[2] = 0;
            header[3] = 0;

            // Text length (4 bytes, big-endian)
            BinaryPrimitives.WriteUInt32BigEndian(header[4..], textLength);

            // Write header to stream
            transport.Stream.Write(header);

            // Write text data to stream
            if (textLength > 0)
                transport.Stream.Write(textBytes);
        }
    }

    /// <summary>
    /// A message for sending clipboard text to the server.
    /// </summary>
    public class ClientCutTextMessage : IOutgoingMessage<ClientCutTextMessageType>
    {
        /// <summary>
        /// Gets the clipboard text to send to the server.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientCutTextMessage"/>.
        /// </summary>
        /// <param name="text">The clipboard text to send.</param>
        public ClientCutTextMessage(string text)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        /// <inheritdoc />
        public string? GetParametersOverview() => $"Text length: {Text.Length} characters";
    }
}
