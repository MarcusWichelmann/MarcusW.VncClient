using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding which declares support for the xvp extension (VM power control).
    /// </summary>
    /// <remarks>
    /// The xvp extension allows the client to request shutdown, reboot, or reset operations
    /// on the remote system (typically a virtual machine).
    /// </remarks>
    public class XvpEncodingType : PseudoEncodingType
    {
        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.Xvp;

        /// <inheritdoc />
        public override string Name => "xvp";

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
        {
            // This is a marker pseudo-encoding, no data is received.
            // The server responds with an XVP_INIT message to confirm support.
        }
    }
}
