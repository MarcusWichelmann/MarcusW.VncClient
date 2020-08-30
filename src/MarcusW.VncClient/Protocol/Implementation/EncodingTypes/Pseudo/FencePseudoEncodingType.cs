using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding type to detect server-side support for fences.
    /// </summary>
    public class FencePseudoEncodingType : PseudoEncodingType
    {
        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.Fence;

        /// <inheritdoc />
        public override string Name => "Fence";

        /// <inheritdoc />
        public override bool GetsConfirmed => true; // The server will send a ServerFence message for confirmation.

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream)
        {
            // Do nothing. This pseudo encoding only exists to check for server-side support.
        }
    }
}
