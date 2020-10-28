namespace MarcusW.VncClient.Protocol.MessageTypes
{
    /// <summary>
    /// Represents a client-to-server message of the RFB protocol.
    /// </summary>
    public interface IOutgoingMessage<out TMessageType> where TMessageType : class, IOutgoingMessageType
    {
        /// <summary>
        /// Returns a string which gives a short overview of the message parameters for logging purposes.
        /// </summary>
        /// <returns>A string with the message parameters or null, if there aren't any.</returns>
        string? GetParametersOverview();
    }
}
