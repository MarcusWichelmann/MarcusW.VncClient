using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding type to detect server-side support for continuous updates.
    /// </summary>
    public class ContinuousUpdatesEncodingType : PseudoEncodingType
    {
        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.ContinuousUpdates;

        /// <inheritdoc />
        public override string Name => "ContinuousUpdates";

        /// <inheritdoc />
        public override bool GetsConfirmed => true; // The server will send a EndOfContinuousUpdates message for confirmation.

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream)
        {
            // Do nothing. This pseudo encoding only exists to check for server-side support.
        }
    }
}
