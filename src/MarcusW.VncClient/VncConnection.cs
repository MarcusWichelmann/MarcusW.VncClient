using MarcusW.VncClient.Rendering;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient
{
    public class VncConnection
    {
        /// <summary>
        /// Gets the logger factory implementation that should be used for creating new loggers.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets the target received frames should be rendered to.
        /// </summary>
        public IRenderTarget RenderTarget { get; }

        internal VncConnection(ILoggerFactory loggerFactory, IRenderTarget renderTarget)
        {
            LoggerFactory = loggerFactory;
            RenderTarget = renderTarget;
        }
    }
}
