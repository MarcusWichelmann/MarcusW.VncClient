using System.Diagnostics.CodeAnalysis;

namespace MarcusW.VncClient.Protocol.EncodingTypes
{
    /// <summary>
    /// The well known encoding types and their IDs.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum WellKnownEncodingType : int
    {
        Raw = 0,
        CopyRect = 1,
        RRE = 2,
        CoRRE = 4,
        Hextile = 5,
        ZLib = 6,
        Tight = 7,
        ZLibHex = 8,
        ZRLE = 16,
        JpegQualityLevelHigh = -23,
        JpegQualityLevelLow = -32,
        DesktopSize = -223,
        LastRect = -224,
        Cursor = -239,
        XCursor = -240,
        CompressionLevelHigh = -247,
        CompressionLevelLow = -256,
        QemuPointerMotionChange = -257,
        QemuExtendedKeyEvent = -258,
        QemuAudio = -259,
        TightPNG = -260,
        QemuLedState = -261,
        DesktopName = -307,
        ExtendedDesktopSize = -308,
        Xvp = -309,
        Fence = -312,
        ContinuousUpdates = -313,
        CursorWithAlpha = -314,
        ExtendedMouseButtons = -316,
        JpegFineGrainedQualityLevelHigh = -412,
        JpegFineGrainedQualityLevelLow = -512,
        JpegSubsamplingLevelLow = -763,
        JpegSubsamplingLevelHigh = -768,
        ExtendedClipboard = unchecked((int)0xc0a1e5ce)
    }
}
