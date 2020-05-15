using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Specifies the parameters for establishing a VNC connection.
    /// </summary>
    public class ConnectParameters
    {
        /// <summary>
        /// Gets or sets the server address and port to connect to.
        /// </summary>
        public IPEndPoint Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the delay between a connection being interrupted and a reconnect starting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of reconnect attempts.
        /// </summary>
        /// <remarks>
        /// Set to <c>-1</c> for not limit.
        /// </remarks>
        public int MaxReconnectAttempts { get; set; }

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
        }

        internal ConnectParameters DeepCopy()
            => new ConnectParameters {
                Endpoint = new IPEndPoint(new IPAddress(Endpoint.Address.GetAddressBytes(), Endpoint.Address.ScopeId),
                    Endpoint.Port),
                ReconnectDelay = ReconnectDelay,
                MaxReconnectAttempts = MaxReconnectAttempts
            };
    }
}
