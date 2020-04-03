using System.Threading.Tasks;
using MarcusW.VncClient.Rendering;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Client for the VNC protocol which allows connecting to remote VNC servers.
    /// </summary>
    public class VncClient
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="VncClient"/>.
        /// </summary>
        /// <param name="loggerFactory">The logger factory implementation that should be used for creating new loggers.</param>
        public VncClient(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Tries to connect to a VNC server and initializes a new connection object.
        /// </summary>
        /// <param name="renderTarget">The target where received frames should be rendered to.</param>
        /// <returns>An initialized <see cref="VncConnection"/> instance</returns>
        public async Task<VncConnection> ConnectAsync(IRenderTarget renderTarget)
        {
            var vncConnection = new VncConnection(_loggerFactory, renderTarget);

            // TODO: Try to connect to server using vncConnection.ConnectWhatever(). Authenticate?

            return vncConnection;
        }
    }
}
