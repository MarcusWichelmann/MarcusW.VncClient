using System;
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

            Span<byte> buffer = stackalloc byte[1024];

            var bytesRead = 0;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                int read = stream.Read(buffer);
                if (read == 0)
                    throw new UnexpectedEndOfStreamException($"Stream reached its end while trying to skip {numBytes} bytes.");

                bytesRead += read;
            }
            while (bytesRead < numBytes);
        }
    }
}
