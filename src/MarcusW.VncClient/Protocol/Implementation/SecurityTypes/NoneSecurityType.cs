using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Security;

namespace MarcusW.VncClient.Protocol.Implementation.SecurityTypes;

/// <summary>
///     A security type without any security.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="NoneSecurityType" />.
/// </remarks>
/// <param name="context">The connection context.</param>
public class NoneSecurityType(RfbConnectionContext context) : ISecurityType
{
    private readonly ProtocolState _state = context.GetState<ProtocolState>();

    /// <inhertitdoc />
    public byte Id => (byte)WellKnownSecurityType.None;

    /// <inhertitdoc />
    public string Name => "None";

    /// <inhertitdoc />
    public int Priority => 1; // Anything is better than nothing. xD

    /// <inhertitdoc />
    public Task<AuthenticationResult> AuthenticateAsync(IAuthenticationHandler authenticationHandler,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Nothing to do.

        // The server will not answer with a SecurityResult message in earlier protocol versions.
        bool expectSecurityResult = _state.ProtocolVersion >= RfbProtocolVersion.RFB_3_8;

        return Task.FromResult(new AuthenticationResult(null, expectSecurityResult));
    }

    /// <inheritdoc />
    public Task ReadServerInitExtensionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
