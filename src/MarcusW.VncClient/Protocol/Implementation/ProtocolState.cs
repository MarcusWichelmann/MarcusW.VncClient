using System;

namespace MarcusW.VncClient.Protocol.Implementation
{
    /// <inhertitdoc />
    public class ProtocolState : IRfbProtocolState
    {
        private readonly RfbConnectionContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolState"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public ProtocolState(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            // TODO: initialize set of USED messages, encodings
        }
    }
}
