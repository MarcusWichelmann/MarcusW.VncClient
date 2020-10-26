using System;
using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding which informs the server about the wished fine-grained JPEG quality level.
    /// </summary>
    public class JpegFineGrainedQualityLevelEncodingType : PseudoEncodingType
    {
        private readonly RfbConnectionContext _context;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.JpegFineGrainedQualityLevelLow + _context.Connection.Parameters.JpegQualityLevel;

        /// <inheritdoc />
        public override string Name => $"JPEG Fine-Grained Quality Level: {_context.Connection.Parameters.JpegQualityLevel}%";

        /// <inheritdoc />
        public override bool GetsConfirmed => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="JpegFineGrainedQualityLevelEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public JpegFineGrainedQualityLevelEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream)
        {
            // Do nothing.
        }
    }
}
