using System.Diagnostics.CodeAnalysis;

namespace MarcusW.VncClient.Protocol.MessageTypes
{
    /// <summary>
    /// The well known outgoing message types and their IDs.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum WellKnownOutgoingMessageType : byte
    {
        SetPixelFormat = 0,
        SetEncodings = 2,
        FramebufferUpdateRequest = 3,
        KeyEvent = 4,
        PointerEvent = 5,
        ClientCutText = 6,
        FileTransfer = 7,
        SetScale = 8,
        SetServerInput = 9,
        SetSW = 10,
        TextChat = 11,
        KeyFrameRequest = 12,
        KeepAlive = 13,
        SetScaleFactor = 15,
        RequestSession = 20,
        SetSession = 21,
        NotifyPluginStreaming = 80,
        EnableContinuousUpdates = 150,
        ClientFence = 248,
        SetDesktopSize = 251
    }
}
