using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Security;

namespace MarcusW.VncClient.Protocol.Implementation.SecurityTypes
{
    /// <summary>
    /// A security type without any security (Security type "None").
    /// </summary>
    public class NoSecurityType : ISecurityType
    {
        /// <inhertitdoc />
        public byte Id => 1;

        /// <inhertitdoc />
        public string Name => "None";

        /// <inhertitdoc />
        public int Priority => 1; // Anything is better than nothing. xD

        /// <inhertitdoc />
        public async Task<AuthenticationResult> AuthenticateAsync(RfbProtocolVersion protocolVersion, IAuthenticationHandler authenticationHandler,
            CancellationToken cancellationToken = default)
        {
            if (!Enum.IsDefined(typeof(RfbProtocolVersion), protocolVersion) || protocolVersion == RfbProtocolVersion.Unknown)
                throw new InvalidEnumArgumentException(nameof(protocolVersion), (int)protocolVersion, typeof(RfbProtocolVersion));
            if (authenticationHandler == null)
                throw new ArgumentNullException(nameof(authenticationHandler));

            cancellationToken.ThrowIfCancellationRequested();

            // Nothing to do.

            // The server will not answer with a SecurityResult message in earlier protocol versions.
            bool expectSecurityResult = protocolVersion >= RfbProtocolVersion.RFB_3_8;

            return new AuthenticationResult(null, expectSecurityResult);
        }
    }
}
