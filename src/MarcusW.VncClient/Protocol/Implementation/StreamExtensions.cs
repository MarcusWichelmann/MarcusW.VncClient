using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusW.VncClient.Protocol.Implementation
{
    /// <summary>
    /// Extension methods for <see cref="Stream"/>.
    /// </summary>
    public static class StreamExtensions
    {
        private const int DefaultBufferSize = 81920;

        /// <summary>
        /// Reads a chunk of bytes from the stream and waits until all bytes are received.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">The buffer to read to.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void ReadAll(this Stream stream, Span<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            int numBytes = buffer.Length;
            if (numBytes == 0)
                return;

            var bytesRead = 0;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                int read = stream.Read(bytesRead == 0 ? buffer : buffer.Slice(bytesRead));
                if (read == 0)
                    throw new UnexpectedEndOfStreamException($"Stream reached its end while trying to read {numBytes} bytes.");

                bytesRead += read;
            }
            while (bytesRead < numBytes);
        }

        /// <summary>
        /// Reads a chunk of bytes from the stream and waits asynchronously until all bytes are received.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="numBytes">The number of bytes to read.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The read bytes.</returns>
        public static async Task<ReadOnlyMemory<byte>> ReadAllAsync(this Stream stream, int numBytes, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            cancellationToken.ThrowIfCancellationRequested();

            var buffer = new byte[numBytes];

            if (numBytes > 0)
            {
                // Read until all bytes are received
                var bytesRead = 0;
                do
                {
                    int read = await stream.ReadAsync(buffer, bytesRead, numBytes - bytesRead, cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                        throw new UnexpectedEndOfStreamException($"Stream reached its end while trying to read {numBytes} bytes.");

                    bytesRead += read;
                }
                while (bytesRead < numBytes);
            }

            return buffer.AsMemory();
        }

        /// <summary>
        /// Reads and discards the given amount of bytes from the stream and waits until all bytes are received.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="numBytes">The number of bytes to skip.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void SkipAll(this Stream stream, int numBytes, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (numBytes == 0)
                return;

            // Rent a buffer
            int bufferSize = numBytes < DefaultBufferSize ? numBytes : DefaultBufferSize;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            Span<byte> bufferSpan = buffer;

            int bytesToSkip = numBytes;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Always slice, because the rented buffer might be larger than requested.
                int read = stream.Read(bufferSpan.Slice(0, Math.Min(bytesToSkip, bufferSize)));
                if (read == 0)
                    throw new UnexpectedEndOfStreamException($"Stream reached its end while trying to skip {numBytes} bytes.");

                bytesToSkip -= read;
            }
            while (bytesToSkip > 0);
        }

        /// <summary>
        /// Reads a specified amount of bytes from the current stream and writes them to another stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="target">The target stream to write to.</param>
        /// <param name="numBytes">The number of bytes to copy.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void CopyAllTo(this Stream stream, Stream target, int numBytes, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (numBytes == 0)
                return;

            // Rent a buffer
            int bufferSize = numBytes < DefaultBufferSize ? numBytes : DefaultBufferSize;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            Span<byte> bufferSpan = buffer;
            try
            {
                int bytesToCopy = numBytes;
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Always slice, because the rented buffer might be larger than requested.
                    int read = stream.Read(bufferSpan.Slice(0, Math.Min(bytesToCopy, bufferSize)));
                    if (read == 0)
                        throw new UnexpectedEndOfStreamException($"Stream reached its end while trying to copy {numBytes} bytes.");

                    target.Write(bufferSpan.Slice(0, read));

                    bytesToCopy -= read;
                }
                while (bytesToCopy > 0);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

#if NETSTANDARD2_0
        public static int Read(this Stream stream, Span<byte> buffer)
        {
            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);

            try
            {
                int numRead = stream.Read(sharedBuffer, 0, buffer.Length);

                if (numRead > buffer.Length)
                {
                    throw new IOException("Bytes read from the stream exceed the size of the buffer");
                }

                new Span<byte>(sharedBuffer, 0, numRead).CopyTo(buffer);

                return numRead;
            }

            finally
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }

        public static void Write(this Stream stream, ReadOnlySpan<byte> buffer)
        {
            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);

            try
            {
                buffer.CopyTo(sharedBuffer);
                stream.Write(sharedBuffer, 0, buffer.Length);
            }

            finally
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }
#endif
    }
}
