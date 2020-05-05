using System;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Rendering;
using MarcusW.VncClient.Utils;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// Receives and processes RFB protocol messages.
    /// </summary>
    public sealed class RfbMessageReceiver : BackgroundThread
    {
        private readonly RfbConnection _connection;
        private readonly ILogger<RfbMessageReceiver> _logger;

        internal RfbMessageReceiver(RfbConnection connection /* TODO: input stream */) : base("RFB Message Receiver")
        {
            _connection = connection;
            _logger = connection.LoggerFactory.CreateLogger<RfbMessageReceiver>();
        }

        internal void StartReceiveLoop(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting receive loop...");
            Start(cancellationToken);
        }

        internal Task StopReceiveLoopAsync()
        {
            _logger.LogDebug("Stopping receive loop...");
            return StopAndWaitAsync();
        }

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
