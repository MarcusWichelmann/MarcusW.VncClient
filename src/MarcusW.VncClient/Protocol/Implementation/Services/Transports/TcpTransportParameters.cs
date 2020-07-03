using System;
using System.Collections.Generic;
using MarcusW.VncClient.Utils;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Transports
{
    /// <summary>
    /// Specifies the parameters for establishing a TCP transport.
    /// </summary>
    public sealed class TcpTransportParameters : TransportParameters
    {
        private string _host = null!;
        private int _port;

        /// <summary>
        /// Gets or sets the name or address of the target host.
        /// </summary>
        public string Host
        {
            get => _host;
            set => ThrowIfFrozen(() => _host = value);
        }

        /// <summary>
        /// Gets or sets the target port.
        /// </summary>
        public int Port
        {
            get => _port;
            set => ThrowIfFrozen(() => _port = value);
        }

        /// <inheritdoc />
        public override void Validate()
        {
            if (Host == null)
                throw new ConnectParametersValidationException($"{nameof(Host)} parameter must not be null.");
            if (Uri.CheckHostName(Host) == UriHostNameType.Unknown)
                throw new ConnectParametersValidationException($"{nameof(Host)} parameter is not a valid IP address, hostname or DNS name.");
            if (Port < 0 || Port > 65535)
                throw new ConnectParametersValidationException($"{nameof(Port)} parameter is an invalid port number.");
        }

        /// <inheritdoc />
        protected override IEnumerable<FreezableParametersObject?>? GetDescendants() => null;

        /// <inheritdoc />
        public override string ToString() => $"vnc://{Host}:{Port}"; // RFC7869
    }
}
