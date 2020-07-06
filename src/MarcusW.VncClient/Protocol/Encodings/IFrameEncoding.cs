namespace MarcusW.VncClient.Protocol.Encodings
{
    /// <summary>
    /// Represents a RFB protocol encoding for (part of) a frame.
    /// </summary>
    public interface IFrameEncoding : IEncoding
    {
        // TODO: Pass information like the PixelFormat as in-reference to safe copies
    }
}
