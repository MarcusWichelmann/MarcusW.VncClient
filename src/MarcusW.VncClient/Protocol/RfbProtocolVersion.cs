using System.Diagnostics.CodeAnalysis;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// The different versions of the RFB protocol.
    /// </summary>
    /// <remarks>
    /// See: https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#protocolversion
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum RfbProtocolVersion
    {
        /// <summary>
        /// RFB 3.3
        /// </summary>
        RFB_3_3 = 33,

        /// <summary>
        /// RFB 3.7
        /// </summary>
        RFB_3_7 = 37,

        /// <summary>
        /// RFB 3.8
        /// </summary>
        RFB_3_8 = 38
    }
}
