using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Rendering;
using MarcusW.VncClient.Utils;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Services.Communication
{
    /// <summary>
    /// Background thread that receives and processes RFB protocol messages.
    /// </summary>
    internal sealed class RfbMessageReceiver : BackgroundThread, IRfbMessageReceiver
    {
        private readonly RfbConnection _connection;
        private readonly ILogger<RfbMessageReceiver> _logger;

        public RfbMessageReceiver(RfbConnection connection /* TODO: input stream */) : base("RFB Message Receiver")
        {
            _connection = connection;
            _logger = connection.LoggerFactory.CreateLogger<RfbMessageReceiver>();

            // Log failure events from background thread base
            Failed += (sender, args) => _logger.LogWarning("Receive loop failed: {exception", args.Exception);
        }

        public void StartReceiveLoop(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting receive loop...");
            Start(cancellationToken);
        }

        public Task StopReceiveLoopAsync()
        {
            _logger.LogDebug("Stopping receive loop...");
            return StopAndWaitAsync();
        }

        // This method will not catch exceptions so the BackgroundThread base class will receive them,
        // raise a "Failure" and trigger a reconnect.
        protected override void ThreadWorker(CancellationToken cancellationToken)
        {
            byte offset = 0;
            bool sign = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                // TODO: Just some testing code...

                IRenderTarget? renderTarget = _connection.RenderTarget;
                if (renderTarget != null)
                {
                    using var framebuffer = renderTarget.GrabFramebufferReference(new FrameSize(256, 256));
                    unsafe
                    {
                        var ptr = (byte*)framebuffer.Address;
                        for (int row = 0; row < 256; row++)
                        for (int col = 0; col < 256; col++)
                        {
                            *ptr++ = (byte)row; // blue
                            *ptr++ = offset; // green
                            *ptr++ = (byte)col; // red
                            *ptr++ = 0xFF; // alpha
                        }
                    }
                }

                if (sign)
                {
                    if (offset == 0)
                        sign = false;
                    else
                        offset--;
                }
                else
                {
                    if (offset == 255)
                        sign = true;
                    else
                        offset++;
                }

                Thread.Sleep(1000 / 60);
            }
        }
    }
}
