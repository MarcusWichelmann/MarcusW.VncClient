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

        public void Validate()
        {
            if (Endpoint == null)
                throw new ConnectParametersValidationException("Endpoint parameter must not be null.");
        }

        internal ConnectParameters DeepCopy()
            => new ConnectParameters {
                Endpoint = new IPEndPoint(new IPAddress(Endpoint.Address.GetAddressBytes(), Endpoint.Address.ScopeId),
                    Endpoint.Port),
                ReconnectDelay = ReconnectDelay
            };
    }
}
