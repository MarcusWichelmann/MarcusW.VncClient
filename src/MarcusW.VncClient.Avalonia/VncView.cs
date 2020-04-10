using MarcusW.VncClient.Avalonia.Adapters.Logging;
using MarcusW.VncClient.Protocol.Encodings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MarcusW.VncClient.Avalonia
{
    /// <summary>
    /// Displays a remote screen using the VNC protocol.
    /// </summary>
    public class VncView : VncRenderTarget
    {
        private readonly VncClient _vncClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="VncView"/> control.
        /// </summary>
        public VncView() : this(InitializeDefaultVncClient()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="VncView"/> control.
        /// </summary>
        /// <param name="vncClient">The configured VNC client that should be used for establishing new connections.</param>
        public VncView(VncClient vncClient)
        {
            _vncClient = vncClient;
        }

        private static VncClient InitializeDefaultVncClient()
        {
            // Create and populate default logger factory for logging to Avalonia logging sinks
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new AvaloniaLoggerProvider());

            return new VncClient(loggerFactory, Defaults.GetEncodingsCollection());
        }
    }
}
