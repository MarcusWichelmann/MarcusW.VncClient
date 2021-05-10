using MarcusW.VncClient.Protocol.MessageTypes;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing
{
    public class ClientCutTextMessageType : IOutgoingMessageType
    {
        public byte Id => (byte) WellKnownOutgoingMessageType.ClientCutText;

        public string Name => "ClientCutText";

        public bool IsStandardMessageType => true;

        public void WriteToTransport(IOutgoingMessage<IOutgoingMessageType> message, ITransport transport, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (!(message is ClientCutTextMessage clientCutMessage))
                throw new ArgumentException($"Message is no {nameof(ClientCutTextMessage)}.", nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            Encoding latin1Encoding = Encoding.GetEncoding("ISO-8859-1");
            int byteCount = latin1Encoding.GetByteCount(clientCutMessage.Text);

            Span<byte> buffer = stackalloc byte[8 + byteCount];

            // Message type
            buffer[0] = Id;

            // Padding
            buffer[1] = 0;
            buffer[2] = 0;
            buffer[3] = 0;

            BinaryPrimitives.WriteUInt32BigEndian(buffer[4..], Convert.ToUInt32(byteCount));
            latin1Encoding.GetBytes(clientCutMessage.Text, buffer[8..]);

            // Write message to stream
            transport.Stream.Write(buffer);
        }
    }

    public class ClientCutTextMessage : IOutgoingMessage<ClientCutTextMessageType>
    {
        public string Text { get; }

        public ClientCutTextMessage(string text)
        {
            Text = text;
        }

        public string? GetParametersOverview() => $"Text: {Text}";
    }

#if NETSTANDARD2_0
    public static class EncodingExtensions
    {
        public static int GetBytes(this Encoding encoding, string input, Span<byte> bytes)
        {
            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(encoding.GetByteCount(input));

            try
            {
                int numRead = encoding.GetBytes(input, 0, input.Length, sharedBuffer, 0);

                if (numRead > bytes.Length)
                {
                    throw new IOException("Bytes read from the stream exceed the size of the buffer");
                }

                new Span<byte>(sharedBuffer, 0, numRead).CopyTo(bytes);

                return numRead;
            }

            finally
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }
    }
#endif
}
