using System;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Protocol.Implementation;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Client for the RFB protocol which allows connecting to remote VNC servers.
    /// </summary>
    public class VncClient
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IRfbProtocolImplementation _protocolImplementation;

        /// <summary>
        /// Initializes a new instance of the <see cref="VncClient"/>.
        /// </summary>
        /// <param name="loggerFactory">The logger factory implementation that should be used for creating new loggers.</param>
        public VncClient(ILoggerFactory loggerFactory) : this(loggerFactory, new DefaultImplementation()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="VncClient"/>.
        /// </summary>
        /// <param name="loggerFactory">The logger factory implementation that should be used for creating new loggers.</param>
        /// <param name="protocolImplementation">The <see cref="IRfbProtocolImplementation"/> that should be used.</param>
        public VncClient(ILoggerFactory loggerFactory, IRfbProtocolImplementation protocolImplementation)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _protocolImplementation = protocolImplementation ?? throw new ArgumentNullException(nameof(protocolImplementation));
        }

        /// <summary>
        /// Tries to connect to a VNC server and initializes a new connection object.
        /// </summary>
        /// <param name="parameters">The connect parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An initialized <see cref="RfbConnection"/> instance.</returns>
        public async Task<RfbConnection> ConnectAsync(ConnectParameters parameters, CancellationToken cancellationToken = default)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            cancellationToken.ThrowIfCancellationRequested();

            // Validate and freeze the parameters so they are immutable from now on
            parameters.ValidateAndFreezeRecursively();

            var rfbConnection = new RfbConnection(_protocolImplementation, _loggerFactory, parameters);
            await rfbConnection.StartAsync(cancellationToken).ConfigureAwait(false);
            return rfbConnection;
        }
    }
}
