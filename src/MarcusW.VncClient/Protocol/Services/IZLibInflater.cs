using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusW.VncClient.Protocol.Services
{
    /// <summary>
    /// Provides methods for inflating (decompressing) received data using a per-connection zlib stream.
    /// </summary>
    public interface IZLibInflater : IDisposable
    {
        /// <summary>
        /// Reads <paramref name="sourceLength"/> bytes from the <paramref name="source"/> stream and returns a stream for inflating them.
        /// </summary>
        /// <remarks>For optimization reasons, this method must not be called again before the reading from the returned stream is over.</remarks>
        /// <param name="source">The stream to read the deflated bytes from.</param>
        /// <param name="sourceLength">The length of the deflated sector in the source stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The inflating stream.</returns>
        Stream ReadAndInflate(Stream source, int sourceLength, CancellationToken cancellationToken = default);
    }
}
