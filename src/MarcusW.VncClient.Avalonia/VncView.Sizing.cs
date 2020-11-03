using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia;
using Avalonia.Controls;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;

namespace MarcusW.VncClient.Avalonia
{
    public partial class VncView
    {
        private enum SizeSource
        {
            None,
            OwnBounds,
            OptimalSizeProperty
        }

        private static readonly TimeSpan ThrottleTime = TimeSpan.FromSeconds(0.5);

        /// <summary>
        /// Defines the <see cref="AutoResizeRemote"/> property.
        /// </summary>
        public static readonly DirectProperty<VncView, bool> AutoResizeRemoteProperty =
            AvaloniaProperty.RegisterDirect<VncView, bool>(nameof(AutoResizeRemote), o => o.AutoResizeRemote, (o, v) => o.AutoResizeRemote = v, true);

        /// <summary>
        /// Defines the <see cref="OptimalSize"/> property.
        /// </summary>
        public static readonly DirectProperty<VncView, global::Avalonia.Size?> OptimalSizeProperty =
            AvaloniaProperty.RegisterDirect<VncView, global::Avalonia.Size?>(nameof(OptimalSize), o => o.OptimalSize, (o, v) => o.OptimalSize = v);

        private bool _autoResizeRemote = true;
        private global::Avalonia.Size? _optimalSize = null;

        private SizeSource _sizeSource = SizeSource.None;
        private IDisposable _sizeSubscription = Disposable.Empty;

        /// <summary>
        /// Gets or sets whether the remote view should be automatically resized to fit the current <see cref="VncView"/>'s size (or the <see cref="OptimalSize"/>).
        /// </summary>
        public bool AutoResizeRemote
        {
            get => _autoResizeRemote;
            set
            {
                if (value)
                    SetSizeSource(OptimalSize == null ? SizeSource.OwnBounds : SizeSource.OptimalSizeProperty);
                else
                    SetSizeSource(SizeSource.None);

                SetAndRaise(AutoResizeRemoteProperty, ref _autoResizeRemote, value);
            }
        }

        /// <summary>
        /// Gets or sets the optimal size value that is used to resize the remote view to make it fit into this optimal size.
        /// If set to <see langword="null"/>, the size of this <see cref="VncView"/> itself is used as the optimal size for the remote view.
        /// </summary>
        /// <remarks>
        /// This property is useful when this <see cref="VncView"/> is contained in e.g. a <see cref="ScrollViewer"/>.
        /// In this case the view would never shrink when this property isn't used.
        /// </remarks>
        public global::Avalonia.Size? OptimalSize
        {
            get => _optimalSize;
            set
            {
                if (AutoResizeRemote)
                    SetSizeSource(value == null ? SizeSource.OwnBounds : SizeSource.OptimalSizeProperty);
                else
                    SetSizeSource(SizeSource.None);

                SetAndRaise(OptimalSizeProperty, ref _optimalSize, value);
            }
        }

        private void InitSizing()
        {
            SetSizeSource(SizeSource.OwnBounds);
        }

        private void SetSizeSource(SizeSource newSizeSource)
        {
            if (newSizeSource == _sizeSource)
                return;
            _sizeSource = newSizeSource;

            // Source has changed, dispose the previous subscription
            _sizeSubscription.Dispose();

            // Disable resizing?
            if (_sizeSource == SizeSource.None)
            {
                _sizeSubscription = Disposable.Empty;
                return;
            }

            // Setup a new subscription
            IObservable<global::Avalonia.Size> observable = _sizeSource == SizeSource.OptimalSizeProperty
                ? this.GetObservable(OptimalSizeProperty).Where(s => s.HasValue).Select(s => s!.Value)
                : this.GetObservable(BoundsProperty).Select(bounds => bounds.Size);
            _sizeSubscription = observable.DistinctUntilChanged().Throttle(ThrottleTime).Subscribe(HandleResize);
        }

        private void HandleResize(global::Avalonia.Size size)
        {
            RfbConnection? connection = Connection;
            if (connection == null)
                return;

            if (!connection.DesktopIsResizable)
                return;

            connection.EnqueueMessage(new SetDesktopSizeMessage((currentSize, currentLayout) => {
                var newSize = new Size((int)size.Width, (int)size.Height);
                var newRectangle = new Rectangle(Position.Origin, newSize);

                Screen newScreen;
                if (!currentLayout.Any())
                {
                    // Create a new layout with one screen
                    newScreen = new Screen(1, newRectangle, 0);
                }
                else
                {
                    // If there is more than one screen, only use one because multi-monitor is not supported
                    Screen firstScreen = currentLayout.First();
                    newScreen = new Screen(firstScreen.Id, newRectangle, firstScreen.Flags);
                }

                return (newSize, new[] { newScreen }.ToImmutableHashSet());
            }));
        }
    }
}
