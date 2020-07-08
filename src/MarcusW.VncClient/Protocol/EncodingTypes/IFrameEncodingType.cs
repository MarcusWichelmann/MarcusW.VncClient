namespace MarcusW.VncClient.Protocol.EncodingTypes
{
    /// <summary>
    /// Represents a RFB protocol encoding type for (part of) a frame.
    /// </summary>
    public interface IFrameEncodingType : IEncodingType
    {
        // TODO: Pass information like the PixelFormat as in-reference to safe copies
    }
}
