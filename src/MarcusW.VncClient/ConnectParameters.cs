using System;
using System.Net;
using MarcusW.VncClient.Rendering;
using MarcusW.VncClient.Security;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Specifies the parameters for establishing a VNC connection.
    /// </summary>
    public class ConnectParameters
    {
        public const int InfiniteReconnects = -1;

        /// <summary>
        /// Gets or sets the server address and port to connect to.
        /// </summary>
        public IPEndPoint? Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the delay between a connection being interrupted and a reconnect starting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the maximum number of reconnect attempts.
        /// </summary>
        /// <remarks>
        /// Set to <c>-1</c> for not limit.
        /// </remarks>
        public int MaxReconnectAttempts { get; set; } = InfiniteReconnects;

        /// <summary>
        /// Gets or sets the <see cref="IAuthenticationHandler"/> implementation to authenticate against the server.
        /// </summary>
        public IAuthenticationHandler? AuthenticationHandler { get; set; }

        /// <summary>
        /// Gets or sets the target where received frames should be rendered to, in case you want to set the target from the start on.
        /// </summary>
        public IRenderTarget? InitialRenderTarget { get; set; }

        /// <summary>
        /// Validates the parameters and throws a <see cref="ConnectParametersValidationException"/> for the first error found.
        /// </summary>
        public void Validate()
        {
            if (Endpoint == null)
                throw new ConnectParametersValidationException($"{nameof(Endpoint)} parameter must not be null.");
            if (MaxReconnectAttempts < -1)
                throw new ConnectParametersValidationException(
                    $"{nameof(MaxReconnectAttempts)} parameter must be set to a positive value, or -1 for no limit.");
            if (AuthenticationHandler == null)
                throw new ConnectParametersValidationException(
                    $"{nameof(AuthenticationHandler)} parameter must not be null.");
        }

        // Always Validate() the object beforehand. Otherwise this might fail.
        internal ConnectParameters DeepCopy()
            => new ConnectParameters {
                Endpoint = new IPEndPoint(new IPAddress(Endpoint!.Address.GetAddressBytes(), Endpoint.Address.ScopeId),
                    Endpoint.Port),
                ConnectTimeout = ConnectTimeout,
                ReconnectDelay = ReconnectDelay,
                MaxReconnectAttempts = MaxReconnectAttempts,
                AuthenticationHandler = AuthenticationHandler,
                InitialRenderTarget = InitialRenderTarget
            };
    }
}
