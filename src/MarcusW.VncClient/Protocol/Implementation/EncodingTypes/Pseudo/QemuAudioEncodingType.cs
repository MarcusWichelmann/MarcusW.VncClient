using System;
using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding which declares support for the QEMU Audio extension.
    /// </summary>
    /// <remarks>
    /// The QEMU Audio extension allows audio streaming from the server to the client.
    /// After receiving confirmation, the client can enable audio capture and receive
    /// audio data from the server.
    /// </remarks>
    public class QemuAudioEncodingType : PseudoEncodingType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<QemuAudioEncodingType> _logger;
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.QemuAudio;

        /// <inheritdoc />
        public override string Name => "QEMU Audio";

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="QemuAudioEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public QemuAudioEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<QemuAudioEncodingType>();
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
        {
            // The server sends an empty pseudo-rectangle to confirm support
            _logger.LogDebug("Server supports QEMU Audio extension");
            _state.ServerSupportsQemuAudio = true;
        }
    }
}
