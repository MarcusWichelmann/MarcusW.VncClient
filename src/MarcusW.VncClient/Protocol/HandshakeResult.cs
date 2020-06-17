using System;
using System.ComponentModel;
using MarcusW.VncClient.Protocol.SecurityTypes;

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
        /// Gets the security type that was used during handshake.
        /// </summary>
        public ISecurityType UsedSecurityType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HandshakeResult"/>.
        /// </summary>
        /// <param name="protocolVersion">The used protocol version.</param>
        /// <param name="usedSecurityType">The used security type.</param>
        public HandshakeResult(RfbProtocolVersion protocolVersion, ISecurityType usedSecurityType)
        {
            if (!Enum.IsDefined(typeof(RfbProtocolVersion), protocolVersion))
                throw new InvalidEnumArgumentException(nameof(protocolVersion), (int)protocolVersion, typeof(RfbProtocolVersion));

            ProtocolVersion = protocolVersion;
            UsedSecurityType = usedSecurityType ?? throw new ArgumentNullException(nameof(usedSecurityType));
        }
    }
}
