using System;
using System.ComponentModel;
using MarcusW.VncClient.Protocol.SecurityTypes;

namespace MarcusW.VncClient.Protocol.Services
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
        /// Gets the new transport, in case a new tunnel has been built during handshake.
        /// </summary>
        public ITransport? TunnelTransport { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HandshakeResult"/>.
        /// </summary>
        /// <param name="protocolVersion">The used protocol version.</param>
        /// <param name="usedSecurityType">The used security type.</param>
        /// <param name="tunnelTransport">The tunnel transport, in case a new tunnel has been built during handshake.</param>
        public HandshakeResult(RfbProtocolVersion protocolVersion, ISecurityType usedSecurityType, ITransport? tunnelTransport)
        {
            if (!Enum.IsDefined(typeof(RfbProtocolVersion), protocolVersion))
                throw new InvalidEnumArgumentException(nameof(protocolVersion), (int)protocolVersion, typeof(RfbProtocolVersion));

            ProtocolVersion = protocolVersion;
            UsedSecurityType = usedSecurityType ?? throw new ArgumentNullException(nameof(usedSecurityType));
            TunnelTransport = tunnelTransport;
        }
    }
}
