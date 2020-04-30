using System;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Utils;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// Receives and processes RFB protocol messages.
    /// </summary>
    public sealed class RfbMessageReceiver : BackgroundThread
    {
        private readonly VncConnection _connection;
        private readonly ILogger<RfbMessageReceiver> _logger;

        internal RfbMessageReceiver(VncConnection connection /* TODO: input stream */) : base("RFB Message Receiver")
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
            while (!cancellationToken.IsCancellationRequested)
            {
                // TODO
                _logger.LogDebug("LOOP");
                Thread.Sleep(1000);
            }
        }
    }
}
