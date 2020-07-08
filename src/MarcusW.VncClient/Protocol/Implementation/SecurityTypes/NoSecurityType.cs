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
        private readonly RfbConnectionContext _context;
        private readonly ProtocolState _state;

        /// <inhertitdoc />
        public byte Id => 1;

        /// <inhertitdoc />
        public string Name => "None";

        /// <inhertitdoc />
        public int Priority => 1; // Anything is better than nothing. xD

        /// <summary>
        /// Initializes a new instance of the <see cref="NoSecurityType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public NoSecurityType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _state = context.GetState<ProtocolState>();
        }

        /// <inhertitdoc />
        public Task<AuthenticationResult> AuthenticateAsync(IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken = default)
        {
            if (authenticationHandler == null)
                throw new ArgumentNullException(nameof(authenticationHandler));

            cancellationToken.ThrowIfCancellationRequested();

            // Nothing to do.

            // The server will not answer with a SecurityResult message in earlier protocol versions.
            bool expectSecurityResult = _state.ProtocolVersion >= RfbProtocolVersion.RFB_3_8;

            return Task.FromResult(new AuthenticationResult(null, expectSecurityResult));
        }

        /// <inheritdoc />
        public Task ReadServerInitExtensionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
