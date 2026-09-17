using System;
using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding which informs the server about the preferred compression level.
    /// </summary>
    /// <remarks>
    /// The compression level is a hint to the server about the tradeoff between CPU time and bandwidth.
    /// Lower levels mean less compression (faster encoding, more bandwidth).
    /// Higher levels mean more compression (slower encoding, less bandwidth).
    /// </remarks>
    public class CompressionLevelEncodingType : PseudoEncodingType
    {
        private readonly RfbConnectionContext _context;

        /// <inheritdoc />
        public override int Id
        {
            get
            {
                int level = _context.Connection.Parameters.PreferredCompressionLevel;
                if (level < 0)
                    level = 6; // Default to moderate compression
                else if (level > 9)
                    level = 9;
                
                // -256 = level 0 (low compression), -247 = level 9 (high compression)
                return (int)WellKnownEncodingType.CompressionLevelLow + level;
            }
        }

        /// <inheritdoc />
        public override string Name
        {
            get
            {
                int level = _context.Connection.Parameters.PreferredCompressionLevel;
                if (level < 0)
                    return "Compression Level: Auto (6)";
                return $"Compression Level: {Math.Min(level, 9)}";
            }
        }

        /// <inheritdoc />
        public override bool GetsConfirmed => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompressionLevelEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public CompressionLevelEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
        {
            // This is a marker pseudo-encoding, no data is received.
        }
    }
}
