using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Utils;

namespace MarcusW.VncClient.Protocol.Services.Communication
{
    /// <summary>
    /// Describes a background thread that receives and processes RFB protocol messages.
    /// </summary>
    internal interface IRfbMessageReceiver : IBackgroundThread
    {
        void StartReceiveLoop(CancellationToken cancellationToken = default);

        Task StopReceiveLoopAsync();
    }
}
