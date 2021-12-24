using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MarcusW.VncClient.Output;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;
using MarcusW.VncClient.Rendering;
using SystemSize = System.Windows.Size;
using VncSize = MarcusW.VncClient.Size;

namespace MarcusW.VncClient.Wpf;

/// <summary>
///     Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
///     Step 1a) Using this custom control in a XAML file that exists in the current project.
///     Add this XmlNamespace attribute to the root element of the markup file where it is
///     to be used:
///     xmlns:MyNamespace="clr-namespace:MarcusW.VncClient.Wpf"
///     Step 1b) Using this custom control in a XAML file that exists in a different project.
///     Add this XmlNamespace attribute to the root element of the markup file where it is
///     to be used:
///     xmlns:MyNamespace="clr-namespace:MarcusW.VncClient.Wpf;assembly=MarcusW.VncClient.Wpf"
///     You will also need to add a project reference from the project where the XAML file lives
///     to this project and Rebuild to avoid compilation errors:
///     Right click on the target project in the Solution Explorer and
///     "Add Reference"->"Projects"->[Select this project]
///     Step 2)
///     Go ahead and use your control in the XAML file.
///     <MyNamespace:CustomControl1 />
/// </summary>
public class VncView : Control, IRenderTarget, IOutputHandler, IDisposable
{
    public const double Dpi = 96;
    private static double _scaling = 1;

    public static readonly DependencyProperty RfbConnectionProperty = DependencyProperty.Register("RfbConnection",
        typeof(RfbConnection), typeof(VncView),
        new PropertyMetadata(default(RfbConnection)) { PropertyChangedCallback = OnRfbConnectionPropertyChanged });

    public static readonly DependencyProperty AutoResizeProperty = DependencyProperty.Register("AutoResize",
        typeof(bool), typeof(VncView),
        new PropertyMetadata(default(bool)) { PropertyChangedCallback = OnAutoResizePropertyChanged });

    private readonly object _bitmapReplacementLock = new();
    private readonly HashSet<KeySymbol> _pressedKeys = new();

    private readonly Subject<SystemSize> _resizeEvents = new();
    private readonly SynchronizationContext? _sync;
    private bool _autoResize;
    private IntPtr _bb;
    private int _bbSize;
    private int _bbStride;
    private volatile WriteableBitmap? _bitmap;

    private volatile bool _disposed;
    private int _height;
    private RfbConnection? _rfbConnection;
    private int _width;

    static VncView()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(VncView), new FrameworkPropertyMetadata(typeof(VncView)));
    }

    public VncView()
    {
        _sync = SynchronizationContext.Current;
        UseLayoutRounding = true;
        _resizeEvents.DistinctUntilChanged().Sample(TimeSpan.FromMilliseconds(50))
            .Subscribe(onNext: OnResizeEventThrottled);
    }

    public bool AutoResize
    {
        get => (bool)GetValue(AutoResizeProperty);
        set => SetValue(AutoResizeProperty, value);
    }

    public RfbConnection? RfbConnection
    {
        get => (RfbConnection?)GetValue(RfbConnectionProperty);
        set => SetValue(RfbConnectionProperty, value);
    }

    public void Dispose() => Dispose(true);

    public void RingBell()
    {
        // Ring the system bell
        Console.Beep();
    }

    public void HandleServerClipboardUpdate(string text)
    {
        _sync?.Post(o => {
            // Copy the text to the local clipboard
            Clipboard.SetText(text);
        }, null);
    }

    public IFramebufferReference GrabFramebufferReference(Size size, IImmutableSet<Screen> layout)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(VncView));
        }

        bool sizeChanged = _bitmap is null || _height != size.Height || _width != size.Width;
        if (sizeChanged)
        {
            lock (_bitmapReplacementLock)
            {
                _width = size.Width;
                _height = size.Height;
                System.Windows.Media.PixelFormat pixelFormat = PixelFormats.Bgr32;
                var bitmap = new WriteableBitmap(_width, _height, Dpi, Dpi, pixelFormat, null);
                _bitmap = bitmap;
                _bitmap.Lock();
                _bb = _bitmap.BackBuffer;
                _bbStride = _bitmap.BackBufferStride;
                _bbSize = _bitmap.PixelHeight * _bbStride;
            }
        }

        var rc = new WpfFramebufferReference(_width, _height, _bb, RenderDone);
        return rc;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.Key == Key.None)
        {
            return;
        }

        KeyModifiers keyModifiers = CollectModifiers();

        // Send key press
        if (!HandleKeyEvent(true, e.Key, keyModifiers))
        {
            return;
        }

        e.Handled = true;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Handled || e.Key == Key.None)
        {
            return;
        }

        KeyModifiers keyModifiers = CollectModifiers();

        // Send key release
        if (!HandleKeyEvent(false, e.Key, keyModifiers))
        {
            return;
        }

        e.Handled = true;
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        ResetKeyPresses();
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        MouseButtons buttonsMask = GetButtonsMask(e);
        HandlePointerEvent(e.GetPosition(this), 0, buttonsMask);

        Focus();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        MouseButtons buttonsMask = GetButtonsMask(e);
        HandlePointerEvent(e.GetPosition(this), 0, buttonsMask);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        MouseButtons buttonsMask = GetButtonsMask(e);
        HandlePointerEvent(e.GetPosition(this), 0, buttonsMask);
    }

    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        // If scroll lock is off, forward wheel events to vnc server instead
        // of to the scroll viewer (by setting e.Handled to true)
        if (!Keyboard.IsKeyToggled(Key.Scroll))
        {
            return;
        }

        MouseButtons buttonsMask = GetButtonsMask(e);
        HandlePointerEvent(e.GetPosition(this), e.Delta, buttonsMask);
        e.Handled = true;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (_bitmap == null)
        {
            return;
        }

        lock (_bitmapReplacementLock)
        {
            System.Windows.Media.PixelFormat pixelFormat = PixelFormats.Bgr32;
            var bitmap = new WriteableBitmap(_width, _height, Dpi, Dpi, pixelFormat, null);
            bitmap.Lock();

            unsafe
            {
                Buffer.MemoryCopy((void*)_bb, (void*)bitmap.BackBuffer, _bbSize, _bbSize);
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));

            bitmap.Unlock();
            drawingContext.DrawImage(bitmap, new Rect(0, 0, _width / _scaling, _height / _scaling));
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        _resizeEvents.OnNext(sizeInfo.NewSize);
        base.OnRenderSizeChanged(sizeInfo);
    }

    protected override SystemSize ArrangeOverride(SystemSize arrangeBounds)
    {
        //_resizeEvents.OnNext(arrangeBounds);
        return base.ArrangeOverride(arrangeBounds);
    }

    protected override void OnTextInput(TextCompositionEventArgs e)
    {
        base.OnTextInput(e);
        if (e.Handled)
        {
            return;
        }

        // Get connection
        RfbConnection? connection = _rfbConnection;
        if (connection == null)
        {
            return;
        }

        // Send chars one by one
        foreach (char c in e.Text)
        {
            KeySymbol keySymbol = KeyMapping.GetSymbolFromChar(c);

            // Press and release key
            if (!connection.EnqueueMessage(new KeyEventMessage(true, keySymbol)))
            {
                break;
            }

            connection.EnqueueMessage(new KeyEventMessage(false, keySymbol));
        }

        e.Handled = true;
    }

    private static MouseButtons GetButtonsMask(MouseEventArgs e)
    {
        var buttonsMask = MouseButtons.None;
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            buttonsMask = buttonsMask | MouseButtons.Left;
        }

        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            buttonsMask = buttonsMask | MouseButtons.Middle;
        }

        if (e.RightButton == MouseButtonState.Pressed)
        {
            buttonsMask = buttonsMask | MouseButtons.Right;
        }

        return buttonsMask;
    }

    private static void OnAutoResizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not VncView v)
        {
            return;
        }

        v.OnAutoResizeChanged((bool)e.NewValue);
    }

    private static void OnRfbConnectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not VncView vncView)
        {
            return;
        }

        vncView.OnRfbConnectionChanged(e.OldValue as RfbConnection, e.NewValue as RfbConnection);
    }

    private KeyModifiers CollectModifiers()
    {
        var rc = KeyModifiers.None;
        if (_pressedKeys.Contains(KeySymbol.Control_L) || _pressedKeys.Contains(KeySymbol.Control_R))
        {
            rc |= KeyModifiers.Control;
        }

        return rc;
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            lock (_bitmapReplacementLock)
                _bitmap = null;
        }

        _disposed = true;
    }

    private bool HandleKeyEvent(bool downFlag, Key key, KeyModifiers keyModifiers)
    {
        // Get connection
        RfbConnection? connection = _rfbConnection;
        if (connection == null)
        {
            return false;
        }

        // Might this key be part of a shortcut? When modifies are present, OnTextInput doesn't get called,
        // so we have to handle printable characters here now, too.
        bool includePrintable = (keyModifiers & KeyModifiers.Control) != 0;

        // Get key symbol
        KeySymbol keySymbol = KeyMapping.GetSymbolFromKey(key, includePrintable);
        if (keySymbol == KeySymbol.Null)
        {
            return false;
        }

        // Send key event to server
        bool queued = connection.EnqueueMessage(new KeyEventMessage(downFlag, keySymbol));

        if (downFlag && queued)
        {
            _pressedKeys.Add(keySymbol);
        }
        else if (!downFlag)
        {
            _pressedKeys.Remove(keySymbol);
        }

        return queued;
    }

    private bool HandlePointerEvent(Point pointerPoint, int wheelDelta, MouseButtons buttonsMask)
    {
        RfbConnection? connection = _rfbConnection;
        if (connection == null)
        {
            return false;
        }

        var position = new Position((int)(pointerPoint.X * _scaling), (int)(pointerPoint.Y * _scaling));

        //MouseButtons wheelMask = GetWheelMask(wheelDelta);

        if (wheelDelta != 0)
        {
            MouseButtons wheelMask = wheelDelta > 0 ? MouseButtons.WheelUp : MouseButtons.WheelDown;
            connection.EnqueueMessage(new PointerEventMessage(position, buttonsMask | wheelMask));
        }

        connection.EnqueueMessage(new PointerEventMessage(position, buttonsMask));

        return true;
    }

    private void OnAutoResizeChanged(bool eNewValue)
    {
        _autoResize = eNewValue;
        if (_autoResize)
        {
            ClearValue(WidthProperty);
            ClearValue(HeightProperty);
        }
        else
        {
            SetValue(WidthProperty, (double)_width/_scaling);
            SetValue(HeightProperty, (double)_height/_scaling);
        }
        UpdateSize();
    }

    private void OnResizeEventThrottled(SystemSize obj)
    {
        _sync?.Post(o => {
            CompositionTarget? source = PresentationSource.FromVisual(this)?.CompositionTarget;
            if (source is not null)
            {
                Matrix m = source.TransformToDevice;
                _scaling = m.M11; // x axis
            }
        }, null);
        SendSetDesktopSize(new VncSize((int)obj.Width, (int)obj.Height));
    }

    private void OnRfbConnectionChanged(RfbConnection? oldValue, RfbConnection? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= OnRfbConnectionPropertyChanged;
            oldValue.RenderTarget = null;
            oldValue.OutputHandler = null;
        }

        if (newValue is not null)
        {
            newValue.PropertyChanged += OnRfbConnectionPropertyChanged;
            newValue.RenderTarget = this;
            newValue.OutputHandler = this;
        }

        _rfbConnection = newValue;
    }

    private void OnRfbConnectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MarcusW.VncClient.RfbConnection.DesktopIsResizable))
        {
            _sync?.Post(o => {
                UpdateSize();
            }, null);
        }
    }

    private void RenderDone()
    {
        _sync?.Post(o => {
            InvalidateMeasure();
            InvalidateVisual();
            if (!_autoResize)
            {
                SetValue(WidthProperty, (double)_width / _scaling);
                SetValue(HeightProperty, (double)_height / _scaling);
            }
        }, null);
    }

    private void ResetKeyPresses()
    {
        // (Still) conneced?
        RfbConnection? connection = _rfbConnection;
        if (connection != null)
        {
            // Clear pressed keys
            foreach (KeySymbol keySymbol in _pressedKeys)
            {
                // If the connection is already dead, don't care about clearing them.
                if (!connection.EnqueueMessage(new KeyEventMessage(false, keySymbol)))
                {
                    break;
                }
            }
        }

        _pressedKeys.Clear();
    }

    private void SendSetDesktopSize(VncSize size)
    {
        RfbConnection? connection = _rfbConnection;
        if (connection == null)
        {
            return;
        }

        if (!connection.DesktopIsResizable || !_autoResize)
        {
            return;
        }

        connection.EnqueueMessage(new SetDesktopSizeMessage((currentSize, currentLayout) => {
            var newSize = new VncSize((int)(size.Width * _scaling), (int)(size.Height * _scaling));
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

    private void UpdateSize()
    {
        if (RfbConnection == null)
        {
            return;
        }

        SendSetDesktopSize(new VncSize((int)ActualWidth, (int)ActualHeight));
    }
}
