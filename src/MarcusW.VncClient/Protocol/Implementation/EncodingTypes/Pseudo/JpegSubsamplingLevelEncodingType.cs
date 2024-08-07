using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo;

/// <summary>
///     A pseudo encoding which informs the server about the wished JPEG subsampling level.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="JpegSubsamplingLevelEncodingType" />.
/// </remarks>
/// <param name="context">The connection context.</param>
public class JpegSubsamplingLevelEncodingType(RfbConnectionContext context) : PseudoEncodingType
{
    /// <inheritdoc />
    public override int Id
        => (int)WellKnownEncodingType.JpegSubsamplingLevelHigh
            + (int)context.Connection.Parameters.JpegSubsamplingLevel;

    /// <inheritdoc />
    public override string Name => $"JPEG Subsampling Level: {context.Connection.Parameters.JpegSubsamplingLevel}";

    /// <inheritdoc />
    public override bool GetsConfirmed => false;

    /// <inheritdoc />
    public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
    {
        // Do nothing.
    }
}
