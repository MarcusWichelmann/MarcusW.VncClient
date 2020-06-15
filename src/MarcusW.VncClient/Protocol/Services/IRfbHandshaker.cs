using System.Threading;
using System.Threading.Tasks;

namespace MarcusW.VncClient.Protocol.Services
{
    /// <summary>
    /// Provides methods for doing a RFB compliant handshake.
    /// </summary>
    public interface IRfbHandshaker
    {
        /// <summary>
        /// Executes a handshake.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Information about the handshake outcome.</returns>
        Task<HandshakeResult> DoHandshakeAsync(CancellationToken cancellationToken = default);
    }
}
