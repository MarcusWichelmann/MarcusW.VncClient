using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol;
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
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            if (supportedEncodings == null)
                throw new ArgumentNullException(nameof(supportedEncodings));

            _supportedEncodings =
                supportedEncodings.ToList()
                    .AsReadOnly(); // TODO: Some other data type that allows faster lookup by encoding type? Array?
        }

        /// <summary>
        /// Tries to connect to a VNC server and initializes a new connection object.
        /// </summary>
        /// <param name="parameters">The connect parameters.</param>
        /// <param name="authenticationHandler">The <see cref="IAuthenticationHandler"/> implementation to authenticate against the server.</param>
        /// <param name="initialRenderTarget">The target where received frames should be rendered to, in case you want to set the target from the start on.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An initialized <see cref="RfbConnection"/> instance.</returns>
        public async Task<RfbConnection> ConnectAsync(ConnectParameters parameters,
            IAuthenticationHandler authenticationHandler, IRenderTarget? initialRenderTarget = null,
            CancellationToken cancellationToken = default)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (authenticationHandler == null)
                throw new ArgumentNullException(nameof(authenticationHandler));

            parameters.Validate();

            // Create a deep copy of the parameters object to make sure connection parameters cannot be changed afterwards.
            var parametersCopy = parameters.DeepCopy();

            var protocolImplementation = new RfbProtocol(_supportedEncodings);

            var rfbConnection = new RfbConnection(protocolImplementation, _loggerFactory, parametersCopy,
                authenticationHandler, initialRenderTarget);
            await rfbConnection.StartAsync(cancellationToken).ConfigureAwait(false);
            return rfbConnection;
        }
    }
}
