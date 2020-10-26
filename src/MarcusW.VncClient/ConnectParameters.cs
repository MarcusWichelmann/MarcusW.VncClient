using System;
using System.Collections.Generic;
using System.Net;
using MarcusW.VncClient.Output;
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
        private IOutputHandler? _initialOutputHandler;
        private int _jpegQualityLevel = 100;
        private JpegSubsamplingLevel _jpegSubsamplingLevel = JpegSubsamplingLevel.None;

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

        /// <summary>
        /// Gets or sets the flags that control, how the rendering to the target framebuffer should happen.
        /// </summary>
        public RenderFlags RenderFlags
        {
            get => _renderFlags;
            set => ThrowIfFrozen(() => _renderFlags = value);
        }

        /// <summary>
        /// Gets or sets the handler for output events from the server, in case you want to set the handler from the start on.
        /// </summary>
        public IOutputHandler? InitialOutputHandler
        {
            get => _initialOutputHandler;
            set => ThrowIfFrozen(() => _initialOutputHandler = value);
        }

        /// <summary>
        /// Gets or sets the JPEG quality level in percent (0 to 100).
        /// </summary>
        public int JpegQualityLevel
        {
            get => _jpegQualityLevel;
            set => ThrowIfFrozen(() => _jpegQualityLevel = value);
        }

        /// <summary>
        /// Gets or sets the JPEG subsampling level.
        /// </summary>
        public JpegSubsamplingLevel JpegSubsamplingLevel
        {
            get => _jpegSubsamplingLevel;
            set => ThrowIfFrozen(() => _jpegSubsamplingLevel = value);
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
            if (!Enum.IsDefined(typeof(RenderFlags), RenderFlags))
                throw new ConnectParametersValidationException($"{nameof(RenderFlags)} parameter is invalid.");
            if (JpegQualityLevel < 0 || JpegQualityLevel > 100)
                throw new ConnectParametersValidationException($"{nameof(JpegQualityLevel)} parameter is not a valid percentage.");
            if (!Enum.IsDefined(typeof(JpegSubsamplingLevel), JpegSubsamplingLevel))
                throw new ConnectParametersValidationException($"{nameof(JpegSubsamplingLevel)} parameter is invalid.");
        }

        /// <inheritdoc />
        protected override IEnumerable<FreezableParametersObject?>? GetDescendants() => new[] { TransportParameters };
    }
}
