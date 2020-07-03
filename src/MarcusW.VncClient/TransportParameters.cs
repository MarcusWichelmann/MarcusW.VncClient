using MarcusW.VncClient.Utils;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Represents parameters for establishing a RFB transport.
    /// </summary>
    public abstract class TransportParameters : FreezableParametersObject
    {
        /// <inheritdoc />
        public abstract override string ToString();
    }
}
