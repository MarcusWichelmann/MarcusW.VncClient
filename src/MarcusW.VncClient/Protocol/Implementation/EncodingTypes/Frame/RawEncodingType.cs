using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Frame
{
    /// <summary>
    /// A frame encoding type for raw pixel data.
    /// </summary>
    public class RawEncodingType : FrameEncodingType
    {
        private const int ChunkSize = 1024 * 16;

        private readonly byte[] _buffer = new byte[ChunkSize];

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.Raw;

        /// <inheritdoc />
        public override string Name => "Raw";

        /// <inheritdoc />
        public override int Priority => 1;

        /// <inheritdoc />
        public override bool GetsConfirmed => false; // All servers support this encoding type.

        /// <inheritdoc />
        public override Color VisualizationColor => new Color(0, 0, 255);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void ReadFrameEncoding(Stream transportStream, IFramebufferReference? targetFramebuffer, in Rectangle rectangle, in Size remoteFramebufferSize,
            in PixelFormat remoteFramebufferFormat)
        {
            if (transportStream == null)
                throw new ArgumentNullException(nameof(transportStream));

            // Calculate how many bytes we're going to receive
            byte bytesPerPixel = remoteFramebufferFormat.BytesPerPixel;
            int totalBytesToRead = rectangle.Size.Width * rectangle.Size.Height * bytesPerPixel;

            // If there is nothing to render to, just skip the received bytes
            if (targetFramebuffer == null)
            {
                transportStream.SkipAll(totalBytesToRead);
                return;
            }

            // Create a cursor for writing single pixels of the rectangle to the target framebuffer
            var framebufferCursor = new FramebufferCursor(targetFramebuffer, rectangle);

            Span<byte> buffer = _buffer.AsSpan();
            int remainingBytesToRead = totalBytesToRead;
            var unprocessedBytesInBuffer = 0;
            do
            {
                // The number of bytes to read for the next chunk
                int bytesToRead = ChunkSize - unprocessedBytesInBuffer;

                // Read less if the end is reached
                if (bytesToRead > remainingBytesToRead)
                {
                    bytesToRead = remainingBytesToRead;
                }

                // Read less if this helps to keep the number of bytes in the buffer a multiple of bytesPerPixel.
                // This saves us the copying of the remaining bytes to the start of the buffer.
                else if (bytesPerPixel > 1)
                {
                    int expectedBufferSize = unprocessedBytesInBuffer + bytesToRead;

                    // Calculate the remainder of a modulo with the bytes per pixel and optimize for the most common pixel formats.
                    int remainder = bytesPerPixel switch {
                        2 => expectedBufferSize & 0b1,
                        4 => expectedBufferSize & 0b11,
                        _ => expectedBufferSize % bytesPerPixel
                    };

                    // Trim the number of bytes to read accordingly
                    if (remainder != 0)
                        bytesToRead -= remainder;
                }

                Span<byte> chunk = bytesToRead < ChunkSize ? buffer.Slice(unprocessedBytesInBuffer, bytesToRead) : buffer;
                int read = transportStream.Read(chunk);

                // Process all available bytes that are sufficient to form full pixels
                int availableBytes = unprocessedBytesInBuffer + read;
                int bytesToProcess = availableBytes;
                unsafe
                {
                    fixed (byte* bufferPtr = buffer)
                    {
                        byte* pixelPtr = bufferPtr;

                        // Process, while there are enough bytes available for the next full pixel
                        while (bytesToProcess >= bytesPerPixel)
                        {
                            // Set the pixel
                            framebufferCursor.SetPixel(pixelPtr, remoteFramebufferFormat);
                            if (!framebufferCursor.GetEndReached())
                                framebufferCursor.MoveNext();

                            // Move forward in buffer
                            pixelPtr += bytesPerPixel;
                            bytesToProcess -= bytesPerPixel;
                        }
                    }
                }

                // Copy all bytes that could not be processed yet to the start of the buffer so we can process them later when more bytes have been received.
                unprocessedBytesInBuffer = bytesToProcess;
                if (unprocessedBytesInBuffer > 0)
                    buffer.Slice(availableBytes - unprocessedBytesInBuffer, unprocessedBytesInBuffer).CopyTo(buffer);

                remainingBytesToRead -= read;
            }
            while (remainingBytesToRead > 0);

            // There shouldn't be any bytes remaining that have not been processed.
            Debug.Assert(unprocessedBytesInBuffer == 0, "bytesRemainingInBuffer == 0");
        }
    }
}
