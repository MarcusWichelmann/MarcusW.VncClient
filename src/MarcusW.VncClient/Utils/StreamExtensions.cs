using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusW.VncClient.Utils
{
    /// <summary>
    /// Extension methods for <see cref="Stream"/>.
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        /// Reads a chunk of bytes from the stream and waits asynchronously until all bytes are received.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="numBytes">The number of bytes to read.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The read bytes.</returns>
        public static async Task<ReadOnlyMemory<byte>> ReadAllBytesAsync(this Stream stream, int numBytes, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var buffer = new byte[numBytes];

            // Read until all bytes are received
            var bytesRead = 0;
            do
            {
                int read = await stream.ReadAsync(buffer, bytesRead, numBytes - bytesRead, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    throw new EndOfStreamException($"Stream reached its end while trying to read {numBytes} bytes.");

                bytesRead += read;
            }
            while (bytesRead < numBytes);

            return buffer.AsMemory();
        }
    }
}