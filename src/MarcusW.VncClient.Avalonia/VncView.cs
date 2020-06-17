using Avalonia;

namespace MarcusW.VncClient.Avalonia
{
    /// <summary>
    /// Displays a remote screen using the RFB protocol.
    /// </summary>
    public class VncView : RfbRenderTarget
    {
        /// <summary>
        /// Defines the <see cref="Connection"/> property.
        /// </summary>
        public static readonly DirectProperty<VncView, RfbConnection?> ConnectionProperty =
            AvaloniaProperty.RegisterDirect<VncView, RfbConnection?>(nameof(Connection), o => o.Connection,
                (o, v) => o.Connection = v);

        private RfbConnection? _connection;

        /// <summary>
        /// Gets or sets the connection that is shown in this VNC view.
        /// </summary>
        /// <remarks>
        /// Interactions with this control will be forwarded to the selected <see cref="RfbConnection"/>.
        /// In case this property is set to <see langword="null"/>, no connection will be attached to this view.
        /// </remarks>
        public RfbConnection? Connection
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
    }
}
