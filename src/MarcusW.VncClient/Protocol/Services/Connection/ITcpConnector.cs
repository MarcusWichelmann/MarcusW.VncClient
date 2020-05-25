using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusW.VncClient.Protocol.Services.Connection
{
    /// <summary>
    /// Provides methods for establishing TCP connections used for the RFB protocol.
    /// </summary>
    public interface ITcpConnector
    {
        /// <summary>
        /// Connects to the specified endpoint and returns the connected <see cref="TcpClient"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The connected client.</returns>
        Task<TcpClient> ConnectAsync(CancellationToken cancellationToken = default);
    }
}
