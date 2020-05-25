using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Services.Handshaking
{
    /// <inheritdoc />
    public class RfbHandshaker : IRfbHandshaker
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<RfbHandshaker> _logger;

        internal RfbHandshaker(RfbConnectionContext context)
        {
            _context = context;
            _logger = context.Connection.LoggerFactory.CreateLogger<RfbHandshaker>();
        }

        /// <inheritdoc />
        public async Task<HandshakeResult> DoHandshakeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return new HandshakeResult(RfbProtocolVersion.RFB_3_8); // TODO
        }

        // TODO: Add methods for each step, reading/sending
    }
}
