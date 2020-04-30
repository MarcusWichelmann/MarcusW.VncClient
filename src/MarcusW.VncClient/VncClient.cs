using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.Encodings;
using MarcusW.VncClient.Rendering;
using MarcusW.VncClient.Security;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Client for the RFB protocol which allows connecting to remote VNC servers.
    /// </summary>
    public class VncClient
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IReadOnlyCollection<IEncoding> _supportedEncodings;

        /// <summary>
        /// Initializes a new instance of the <see cref="VncClient"/>.
        /// </summary>
        /// <param name="loggerFactory">The logger factory implementation that should be used for creating new loggers.</param>
        /// <param name="supportedEncodings">The collection of supported encodings.</param>
        public VncClient(ILoggerFactory loggerFactory, IEnumerable<IEncoding> supportedEncodings)
        {
            _loggerFactory = loggerFactory;
            _supportedEncodings =
                supportedEncodings.ToList()
                    .AsReadOnly(); // TODO: Some other data type that allows faster lookup by encoding type? Array?
        }

        /// <summary>
        /// Tries to connect to a VNC server and initializes a new connection object.
        /// </summary>
        /// <param name="authenticationHandler">The <see cref="IAuthenticationHandler"/> implementation to authenticate against the server.</param>
        /// <param name="initialRenderTarget">The target where received frames should be rendered to, in case you want to set the target from the start on.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An initialized <see cref="VncConnection"/> instance.</returns>
        public async Task<VncConnection> ConnectAsync(IAuthenticationHandler authenticationHandler,
            IRenderTarget? initialRenderTarget = null, CancellationToken cancellationToken = default)
        {
            var vncConnection = new VncConnection(_loggerFactory, _supportedEncodings, authenticationHandler,
                initialRenderTarget);
            await vncConnection.StartAsync(cancellationToken).ConfigureAwait(false);
            return vncConnection;
        }
    }
}
