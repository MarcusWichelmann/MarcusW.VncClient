using Avalonia;
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
        /// <summary>
        /// Defines the <see cref="Connection"/> property.
        /// </summary>
        public static readonly DirectProperty<VncView, VncConnection?> ConnectionProperty =
            AvaloniaProperty.RegisterDirect<VncView, VncConnection?>(nameof(Connection), o => o.Connection,
                (o, v) => o.Connection = v);

        private VncConnection? _connection;

        /// <summary>
        /// Gets or sets the connection that is shown in this VNC view.
        /// </summary>
        /// <remarks>
        /// Interactions with this control will be forwarded to the selected <see cref="VncConnection"/>.
        /// In case this property is set to <see langword="null"/>, no connection will be attached to this view.
        /// </remarks>
        public VncConnection? Connection
        {
            get => _connection;
            set
            {
                // Detach view from previous connection
                if (_connection != null && ReferenceEquals(_connection.RenderTarget, this))
                    _connection.RenderTarget = null;

                // Attach view to new connection
                if (value != null)
                    value.RenderTarget = this;

                SetAndRaise(ConnectionProperty, ref _connection, value);
            }
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
