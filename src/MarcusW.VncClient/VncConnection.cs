using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Connection to a remote server using the VNC protocol.
    /// </summary>
    public class VncConnection
    {
        private readonly ILogger<VncConnection> _logger;

        private RfbMessageReceiver? _messageReceiver;

        /// <summary>
        /// Gets the logger factory implementation that should be used for creating new loggers.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets the collection of supported encodings.
        /// </summary>
        public IReadOnlyCollection<IEncoding> SupportedEncodings { get; }

        /// <summary>
        /// Gets the authentication handler.
        /// </summary>
        public IAuthenticationHandler AuthenticationHandler { get; }

        /// <summary>
        /// Gets or sets the target where received frames should be rendered to.
        /// </summary>
        public IRenderTarget? RenderTarget { get; set; }

        // TODO: ConnectionState property and event (informs about reconnects)

        internal VncConnection(ILoggerFactory loggerFactory, IReadOnlyCollection<IEncoding> supportedEncodings,
            IAuthenticationHandler authenticationHandler, IRenderTarget? initialRenderTarget = null)
        {
            LoggerFactory = loggerFactory;
            SupportedEncodings = supportedEncodings;
            AuthenticationHandler = authenticationHandler;
            RenderTarget = initialRenderTarget;

            _logger = loggerFactory.CreateLogger<VncConnection>();
        }

        internal async Task StartAsync(CancellationToken cancellationToken = default)
        {
            // TODO: Add server address to log output
            _logger.LogInformation("Connecting to VNC-Server XXX...");

            // TODO: Connect and authenticate
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);

            _messageReceiver = new RfbMessageReceiver(this);
            _messageReceiver.StartReceiveLoop(cancellationToken);
        }

        /// <summary>
        /// Closes the running remote connection.
        /// </summary>
        /// <remarks>
        /// To cancel the connection establishment please use the <see cref="CancellationToken"/> passed to <see cref="StartAsync"/> instead.
        /// </remarks>
        public Task CloseAsync()
        {
            // TODO: Throw if connection establishment not finished (using ConnectionState).

            // TODO: Add server address to log output
            _logger.LogInformation("Closing connection to XXX...");

            Debug.Assert(_messageReceiver != null, nameof(_messageReceiver) + " != null");
            return _messageReceiver.StopReceiveLoopAsync();
        }
    }
}
