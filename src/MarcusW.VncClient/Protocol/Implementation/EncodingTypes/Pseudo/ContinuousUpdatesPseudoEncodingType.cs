using System.IO;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    public class ContinuousUpdatesPseudoEncodingType : PseudoEncodingType
    {
        /// <inheritdoc />
        public override int Id => -313;

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
