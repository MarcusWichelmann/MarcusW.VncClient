using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Protocol.Encodings;
using MarcusW.VncClient.Protocol.Implementation;
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
        private readonly IRfbProtocolImplementation _protocolImplementation;

        /// <summary>
        /// Initializes a new instance of the <see cref="VncClient"/>.
        /// </summary>
        /// <param name="loggerFactory">The logger factory implementation that should be used for creating new loggers.</param>
        public VncClient(ILoggerFactory loggerFactory):this(loggerFactory,new DefaultImplementation()){}

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
        public async Task<RfbConnection> ConnectAsync(ConnectParameters parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            cancellationToken.ThrowIfCancellationRequested();

            parameters.Validate();

            // Create a deep copy of the parameters object to make sure connection parameters cannot be changed afterwards.
            var parametersCopy = parameters.DeepCopy();

            var rfbConnection = new RfbConnection(_protocolImplementation, _loggerFactory, parametersCopy);
            await rfbConnection.StartAsync(cancellationToken).ConfigureAwait(false);
            return rfbConnection;
        }
    }
}
