using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing
{
    /// <summary>
    /// A message type for SetPixelFormat messages.
    /// </summary>
    public class SetPixelFormatMessageType : IOutgoingMessageType
    {
        /// <inheritdoc />
        public byte Id => (byte)WellKnownOutgoingMessageType.SetPixelFormat;

        /// <inheritdoc />
        public string Name => "SetPixelFormat";

        /// <inheritdoc />
        public bool IsStandardMessageType => true;

        /// <inheritdoc />
        public void WriteToTransport(IOutgoingMessage<IOutgoingMessageType> message, ITransport transport, CancellationToken cancellationToken = default)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (!(message is SetPixelFormatMessage setPixelFormatMessage))
                throw new ArgumentException($"Message is not a {nameof(SetPixelFormatMessage)}.", nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            // Calculate message size: message type (1) + padding (3) + pixel format (16) = 20 bytes
            const int messageSize = 20;
            Span<byte> buffer = stackalloc byte[messageSize];

            // Write message type
            buffer[0] = Id;

            // Write padding (3 bytes)
            buffer[1] = 0;
            buffer[2] = 0;
            buffer[3] = 0;

            // Write pixel format (16 bytes)
            SerializePixelFormat(setPixelFormatMessage.PixelFormat, buffer[4..]);

            // Write buffer to stream
            transport.Stream.Write(buffer);
        }

        private static void SerializePixelFormat(PixelFormat pixelFormat, Span<byte> buffer)
        {
            buffer[0] = pixelFormat.BitsPerPixel;
            buffer[1] = pixelFormat.Depth;
            buffer[2] = (byte)(pixelFormat.BigEndian ? 1 : 0);
            buffer[3] = (byte)(pixelFormat.TrueColor ? 1 : 0);
            
            // Red max (big-endian)
            buffer[4] = (byte)(pixelFormat.RedMax >> 8);
            buffer[5] = (byte)(pixelFormat.RedMax & 0xFF);
            
            // Green max (big-endian)
            buffer[6] = (byte)(pixelFormat.GreenMax >> 8);
            buffer[7] = (byte)(pixelFormat.GreenMax & 0xFF);
            
            // Blue max (big-endian)  
            buffer[8] = (byte)(pixelFormat.BlueMax >> 8);
            buffer[9] = (byte)(pixelFormat.BlueMax & 0xFF);
            
            buffer[10] = pixelFormat.RedShift;
            buffer[11] = pixelFormat.GreenShift;
            buffer[12] = pixelFormat.BlueShift;
            
            // Padding (3 bytes)
            buffer[13] = 0;
            buffer[14] = 0;
            buffer[15] = 0;
        }
    }
}
