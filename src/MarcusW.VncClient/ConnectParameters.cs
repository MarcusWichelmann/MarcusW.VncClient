using System;
using System.Collections.Generic;
using System.Net;
using MarcusW.VncClient.Rendering;
using MarcusW.VncClient.Security;
using MarcusW.VncClient.Utils;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Specifies the parameters for establishing a VNC connection.
    /// </summary>
    public sealed class ConnectParameters : FreezableParametersObject
    {
        /// <summary>
        /// The value to specify unlimited reconnect attempts.
        /// </summary>
        public const int InfiniteReconnects = -1;

        private TransportParameters _transportParameters = null!;
        private TimeSpan _connectTimeout = TimeSpan.FromSeconds(30);
        private TimeSpan _reconnectDelay = TimeSpan.FromSeconds(5);
        private int _maxReconnectAttempts = InfiniteReconnects;
        private IAuthenticationHandler _authenticationHandler = null!;
        private bool _allowSharedConnection = true;
        private IRenderTarget? _initialRenderTarget;
        private RenderFlags _renderFlags = RenderFlags.Default;

        /// <summary>
        /// Specifies the transport type and parameters to connect to.
        /// </summary>
        public TransportParameters TransportParameters
        {
            get => _transportParameters;
            set => ThrowIfFrozen(() => _transportParameters = value);
        }

        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        public TimeSpan ConnectTimeout
        {
            get => _connectTimeout;
            set => ThrowIfFrozen(() => _connectTimeout = value);
        }

        /// <summary>
        /// Gets or sets the delay between a connection being interrupted and a reconnect starting.
        /// </summary>
        public TimeSpan ReconnectDelay
        {
            get => _reconnectDelay;
            set => ThrowIfFrozen(() => _reconnectDelay = value);
        }

        /// <summary>
        /// Gets or sets the maximum number of reconnect attempts.
        /// </summary>
        /// <remarks>
        /// Set to <c>-1</c> for not limit.
        /// </remarks>
        public int MaxReconnectAttempts
        {
            get => _maxReconnectAttempts;
            set => ThrowIfFrozen(() => _maxReconnectAttempts = value);
        }

        /// <summary>
        /// Gets or sets the <see cref="IAuthenticationHandler"/> implementation that provides information for authenticating against the server.
        /// </summary>
        public IAuthenticationHandler AuthenticationHandler
        {
            get => _authenticationHandler;
            set => ThrowIfFrozen(() => _authenticationHandler = value);
        }

        /// <summary>
        /// Gets or sets whether the server should leave other clients connected when this connection is established.
        /// </summary>
        public bool AllowSharedConnection
        {
            get => _allowSharedConnection;
            set => ThrowIfFrozen(() => _allowSharedConnection = value);
        }

        /// <summary>
        /// Gets or sets the target where received frames should be rendered to, in case you want to set the target from the start on.
        /// </summary>
        public IRenderTarget? InitialRenderTarget
        {
            get => _initialRenderTarget;
            set => ThrowIfFrozen(() => _initialRenderTarget = value);
        }

        public RenderFlags RenderFlags
        {
            get => _renderFlags;
            set => ThrowIfFrozen(() => _renderFlags = value);
        }

        /// <inhertitdoc />
        public override void Validate()
        {
            if (TransportParameters == null)
                throw new ConnectParametersValidationException($"{nameof(TransportParameters)} parameter must not be null.");
            if (MaxReconnectAttempts < -1)
                throw new ConnectParametersValidationException($"{nameof(MaxReconnectAttempts)} parameter must be set to a positive value, or -1 for no limit.");
            if (AuthenticationHandler == null)
                throw new ConnectParametersValidationException($"{nameof(AuthenticationHandler)} parameter must not be null.");
            if (!Enum.IsDefined(typeof(RenderFlags), _renderFlags))
                throw new ConnectParametersValidationException($"{nameof(RenderFlags)} parameter is invalid.");
        }

        /// <inheritdoc />
        protected override IEnumerable<FreezableParametersObject?>? GetDescendants() => new[] { TransportParameters };
    }
}
