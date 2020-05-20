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
        /// <param name="supportedEncodings">The collection of supported encodings or <see langword="null"/> to use the default <see cref="VncDefaults.GetEncodingsCollection"/>.</param>
        public VncClient(ILoggerFactory loggerFactory, IEnumerable<IEncoding>? supportedEncodings = null)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            // TODO: Some other data type that allows faster lookup by encoding type? Array?
            _supportedEncodings = (supportedEncodings ?? VncDefaults.GetEncodingsCollection()).ToList().AsReadOnly();
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

            parameters.Validate();

            // Create a deep copy of the parameters object to make sure connection parameters cannot be changed afterwards.
            var parametersCopy = parameters.DeepCopy();

            var protocolImplementation = new RfbProtocol(_supportedEncodings);

            var rfbConnection = new RfbConnection(protocolImplementation, _loggerFactory, parametersCopy);
            await rfbConnection.StartAsync(cancellationToken).ConfigureAwait(false);
            return rfbConnection;
        }
    }
}
