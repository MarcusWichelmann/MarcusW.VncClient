using System;
using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding which informs the server about the wished JPEG quality level.
    /// </summary>
    public class JpegQualityLevelEncodingType : PseudoEncodingType
    {
        private readonly RfbConnectionContext _context;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.JpegQualityLevelLow + RoundQualityLevel(_context.Connection.Parameters.JpegQualityLevel) - 1;

        /// <inheritdoc />
        public override string Name => $"JPEG Quality Level: {RoundQualityLevel(_context.Connection.Parameters.JpegQualityLevel)}/10";

        /// <inheritdoc />
        public override bool GetsConfirmed => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="JpegQualityLevelEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public JpegQualityLevelEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
        {
            // Do nothing.
        }

        private int RoundQualityLevel(int level)
        {
            int rounded = (int)Math.Round((double)level / 10);
            if (rounded == 0)
                rounded = 1;
            return rounded;
        }
    }
}
