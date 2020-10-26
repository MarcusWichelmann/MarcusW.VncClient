using System;
using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding which informs the server about the wished JPEG subsampling level.
    /// </summary>
    public class JpegSubsamplingLevelEncodingType : PseudoEncodingType
    {
        private readonly RfbConnectionContext _context;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.JpegSubsamplingLevelHigh + (int)_context.Connection.Parameters.JpegSubsamplingLevel;

        /// <inheritdoc />
        public override string Name => $"JPEG Subsampling Level: {_context.Connection.Parameters.JpegSubsamplingLevel}";

        /// <inheritdoc />
        public override bool GetsConfirmed => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="JpegSubsamplingLevelEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public JpegSubsamplingLevelEncodingType(RfbConnectionContext context)
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
