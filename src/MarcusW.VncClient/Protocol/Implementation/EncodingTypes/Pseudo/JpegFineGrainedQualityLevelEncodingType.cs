using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo;

/// <summary>
///     A pseudo encoding which informs the server about the wished fine-grained JPEG quality level.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="JpegFineGrainedQualityLevelEncodingType" />.
/// </remarks>
/// <param name="context">The connection context.</param>
public class JpegFineGrainedQualityLevelEncodingType(RfbConnectionContext context) : PseudoEncodingType
{
    /// <inheritdoc />
    public override int Id
        => (int)WellKnownEncodingType.JpegFineGrainedQualityLevelLow + context.Connection.Parameters.JpegQualityLevel;

    /// <inheritdoc />
    public override string Name
        => $"JPEG Fine-Grained Quality Level: {context.Connection.Parameters.JpegQualityLevel}%";

    /// <inheritdoc />
    public override bool GetsConfirmed => false;

    /// <inheritdoc />
    public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
    {
        // Do nothing.
    }
}
