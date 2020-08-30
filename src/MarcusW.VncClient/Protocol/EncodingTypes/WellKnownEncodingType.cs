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
        DesktopSize = -223,
        LastRect = -224,
        Cursor = -239,
        XCursor = -240,
        TightPNG = -260,
        DesktopName = -307,
        ExtendedDesktopSize = -308,
        Fence = -312,
        ContinuousUpdates = -313,
        CursorWithAlpha = -314
    }
}
