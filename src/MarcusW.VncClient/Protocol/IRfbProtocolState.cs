namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// Represents a class that holds all the variable state information for the RFB protocol implementation.
    /// </summary>
    public interface IRfbProtocolState
    {
        /// <summary>
        /// Prepares the state object for first use.
        /// </summary>
        void Prepare();
    }
}
