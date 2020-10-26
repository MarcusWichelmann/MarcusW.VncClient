using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusW.VncClient.Protocol.Services
{
    /// <summary>
    /// Provides methods for inflating (decompressing) received data using per-connection zlib streams.
    /// </summary>
    /// <remarks>
    /// It's not necessary that implementations of this interface are thread-safe because the processing of received frames always happens synchronously in a single thread.
    /// Because of this, calls to methods of such implementations should never come from multiple threads.
    /// </remarks>
    public interface IZLibInflater : IDisposable
    {
        /// <summary>
        /// Reads <paramref name="sourceLength"/> bytes from the <paramref name="source"/> stream and returns a stream for inflating them.
        /// </summary>
        /// <remarks>For optimization reasons, this method must not be called again before the reading from the returned stream is over.</remarks>
        /// <param name="source">The stream to read the deflated bytes from.</param>
        /// <param name="sourceLength">The length of the deflated sector in the source stream.</param>
        /// <param name="zlibStreamId">The zlib stream to be used for inflating or <c>-1</c> to select the default stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The inflating stream.</returns>
        Stream ReadAndInflate(Stream source, int sourceLength, int zlibStreamId = -1, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets the zlib stream for the given selector.
        /// </summary>
        /// <param name="id">The zlib stream to reset.</param>
        void ResetZlibStream(int id);
    }
}
