using MarcusW.VncClient.Protocol.Services.Communication;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// Provides access to different elements of a RFB protocol implementation.
    /// </summary>
    internal interface IRfbProtocolImplementation
    {
        IRfbMessageReceiver CreateMessageReceiver(RfbConnection connection);

        IRfbMessageSender CreateMessageSender(RfbConnection connection);
    }
}
