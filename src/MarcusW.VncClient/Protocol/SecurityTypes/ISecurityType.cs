using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Security;

namespace MarcusW.VncClient.Protocol.SecurityTypes
{
    /// <summary>
    /// Represents a RFB protocol security type.
    /// </summary>
    public interface ISecurityType
    {
        /// <summary>
        /// Gets the ID for this security type.
        /// </summary>
        byte Id { get; }

        /// <summary>
        /// Gets a human readable name for this security type.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the priority value that represents the quality & security of this security type.
        /// When multiple security types are available, the one with the highest priority value will be chosen.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Executes the security type authentication.
        /// </summary>
        /// <param name="protocolVersion">The protocol version negotiated earlier during the handshake.</param>
        /// <param name="authenticationHandler">The authentication handler to request login data from the application.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The authentication result.</returns>
        Task<AuthenticationResult> AuthenticateAsync(RfbProtocolVersion protocolVersion, IAuthenticationHandler authenticationHandler,
            CancellationToken cancellationToken = default);
    }
}
