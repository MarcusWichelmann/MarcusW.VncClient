using System.Diagnostics.CodeAnalysis;

namespace MarcusW.VncClient.Protocol.MessageTypes
{
    /// <summary>
    /// The well known incoming message types and their IDs.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum WellKnownIncomingMessageType : byte
    {
        FramebufferUpdate = 0,
        SetColourMapEntries = 1,
        Bell = 2,
        ServerCutText = 3,
        ResizeFrameBuffer = 4,
        KeyFrameUpdate = 5,
        FileTransfer = 7,
        TextChat = 11,
        KeepAlive = 13,
        EndOfContinuousUpdates = 150,
        ServerState = 173,
        ServerFence = 248
    }
}
