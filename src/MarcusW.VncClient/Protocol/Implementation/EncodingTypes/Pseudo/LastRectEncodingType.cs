using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding type to detect the last rectangle in a framebuffer update message.
    /// </summary>
    public class LastRectEncodingType : PseudoEncodingType, ILastRectEncodingType
    {
        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.LastRect;

        /// <inheritdoc />
        public override string Name => "LastRect";

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
        {
            // Do nothing. The FramebufferUpdate message type will detect that this encoding implements ILastRectEncodingType.
        }
    }
}
