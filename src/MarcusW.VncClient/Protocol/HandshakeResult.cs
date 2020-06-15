using System;
using System.ComponentModel;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// Provides information about the outcome of a client-server handshake.
    /// </summary>
    public class HandshakeResult
    {
        /// <summary>
        /// Gets the used protocol version.
        /// </summary>
        public RfbProtocolVersion ProtocolVersion { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HandshakeResult"/>.
        /// </summary>
        /// <param name="protocolVersion">The used protocol version.</param>
        public HandshakeResult(RfbProtocolVersion protocolVersion)
        {
            if (!Enum.IsDefined(typeof(RfbProtocolVersion), protocolVersion))
                throw new InvalidEnumArgumentException(nameof(protocolVersion), (int)protocolVersion,
                    typeof(RfbProtocolVersion));

            ProtocolVersion = protocolVersion;
        }
    }
}
