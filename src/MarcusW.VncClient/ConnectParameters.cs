using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Specifies the parameters for establishing a VNC connection.
    /// </summary>
    [Serializable]
    public class ConnectParameters
    {
        private IPEndPoint _endpoint;
        private TimeSpan _reconnectDelay;

        /// <summary>
        /// Gets or sets the server address and port to connect to.
        /// </summary>
        public IPEndPoint Endpoint
        {
            get => _endpoint;
            set => _endpoint = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the delay between a connection being interrupted and a reconnect starting.
        /// </summary>
        public TimeSpan ReconnectDelay
        {
            get => _reconnectDelay;
            set => _reconnectDelay = value;
        }

        public void Validate()
        {
            if (Endpoint == null)
                throw new ConnectParametersValidationException("Endpoint parameter must not be null.");
        }

        internal ConnectParameters DeepCopy()
        {
            // Serialize and deserialize the object to perform a deep copy of all members
            var formatter = new BinaryFormatter();
            using var stream = new MemoryStream();
            formatter.Serialize(stream, this);
            stream.Seek(0, SeekOrigin.Begin);
            return (ConnectParameters)formatter.Deserialize(stream);
        }
    }
}
