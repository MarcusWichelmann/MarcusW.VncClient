using System.Threading;
using System.Threading.Tasks;

namespace MarcusW.VncClient.Protocol.Services
{
    /// <summary>
    /// Provides methods for establishing transport connections used for the RFB protocol.
    /// </summary>
    public interface ITransportConnector
    {
        /// <summary>
        /// Connects to the specified endpoint and returns the resulting <see cref="ITransport"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The open transport.</returns>
        Task<ITransport> ConnectAsync(CancellationToken cancellationToken = default);
    }
}
