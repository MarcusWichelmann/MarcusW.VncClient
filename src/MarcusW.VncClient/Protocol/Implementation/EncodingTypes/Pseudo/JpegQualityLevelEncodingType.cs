using System;
using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo;

/// <summary>
///     A pseudo encoding which informs the server about the wished JPEG quality level.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="JpegQualityLevelEncodingType" />.
/// </remarks>
/// <param name="context">The connection context.</param>
public class JpegQualityLevelEncodingType(RfbConnectionContext context) : PseudoEncodingType
{
    /// <inheritdoc />
    public override int Id
        => (int)WellKnownEncodingType.JpegQualityLevelLow
            + RoundQualityLevel(context.Connection.Parameters.JpegQualityLevel) - 1;

    /// <inheritdoc />
    public override string Name
        => $"JPEG Quality Level: {RoundQualityLevel(context.Connection.Parameters.JpegQualityLevel)}/10";

    /// <inheritdoc />
    public override bool GetsConfirmed => false;

    /// <inheritdoc />
    public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
    {
        // Do nothing.
    }

    private static int RoundQualityLevel(int level)
    {
        var rounded = (int)Math.Round((double)level / 10);
        if (rounded == 0)
        {
            rounded = 1;
        }

        return rounded;
    }
}
