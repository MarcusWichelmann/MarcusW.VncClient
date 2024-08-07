using System.Collections.Immutable;
using System.Drawing;
using System.Reactive.Subjects;
using JetBrains.Annotations;
using MarcusW.VncClient.Output;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;
using MarcusW.VncClient.Rendering;
using MarcusW.VncClient.Skia.Models;
using SkiaSharp;

namespace MarcusW.VncClient.Skia;

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
///     &lt;MyNamespace:CustomControl1 /&gt;
/// </summary>
[PublicAPI]
public class VncView : IRenderTarget, IOutputHandler, IDisposable
{
    public const double Dpi = 96;
    private static readonly double Scaling = 1;
    private readonly bool _autoResize;

    private readonly object _bitmapReplacementLock = new();
    private readonly HashSet<KeySymbol> _pressedKeys = [];

    private readonly Subject<SKSize> _resizeEvents = new();
    private readonly SynchronizationContext? _sync = SynchronizationContext.Current;
    private IntPtr _bb;
    private int _bbSize;
    private int _bbStride;

    private volatile bool _disposed;
    private int _height;
    private int _width;

    public SKBitmap? Bitmap { get; private set; }

    public RfbConnection? RfbConnection { get; set; }

    public void Dispose() => Dispose(true);

    public void RingBell()
    {
        // Ring the system bell
        Console.Beep();
    }

    public void HandleServerClipboardUpdate(string text)
    {
        Clipboard.SetText(text);
    }

    public IFramebufferReference GrabFramebufferReference(Size size, IImmutableSet<Screen> layout)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(VncView));

        bool sizeChanged = Bitmap is null || _height != size.Height || _width != size.Width;
        if (sizeChanged)
        {
            lock (_bitmapReplacementLock)
            {
                _width = size.Width;
                _height = size.Height;
                var pixelFormat = SKColorType.Bgra8888;
                var bitmap = new SKBitmap(new SKImageInfo(_width, _height, pixelFormat, SKAlphaType.Opaque));
                Bitmap = bitmap;
                _bb = bitmap.GetPixels();

                _bbStride = Bitmap.RowBytes;
                _bbSize = Bitmap.ByteCount;
            }
        }

        var rc = new SkiaFramebufferReference(_width, _height, _bb);
        return rc;
    }

    public bool OnKeyDown(Key eKey)
    {
        if (eKey == Key.None)
        {
            return false;
        }

        KeyModifiers keyModifiers = CollectModifiers();

        // Send key press
        return HandleKeyEvent(true, eKey, keyModifiers);
    }

    public bool OnKeyUp(Key eKey)
    {
        if (eKey == Key.None)
        {
            return false;
        }

        KeyModifiers keyModifiers = CollectModifiers();

        // Send key release
        return HandleKeyEvent(false, eKey, keyModifiers);
    }

    public bool OnMouse(Point position, MouseButtons buttonsMask) => HandlePointerEvent(position, 0, buttonsMask);

    public bool OnPreviewMouseWheel(Point position, MouseButtons buttonsMask, int delta)
        => HandlePointerEvent(position, delta, buttonsMask);

    public bool OnTextInput(string eText)
    {
        // Get connection
        RfbConnection? connection = RfbConnection;
        if (connection == null)
        {
            return false;
        }

        // Send chars one by one
        foreach (char c in eText)
        {
            KeySymbol keySymbol = KeyMapping.GetSymbolFromChar(c);

            // Press and release key
            if (!connection.EnqueueMessage(new KeyEventMessage(true, keySymbol)))
            {
                break;
            }

            connection.EnqueueMessage(new KeyEventMessage(false, keySymbol));
        }

        return true;
    }

    public void ResetKeyPresses()
    {
        // (Still) conneced?
        RfbConnection? connection = RfbConnection;
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

    public void SendSetDesktopSize(Size size)
    {
        RfbConnection? connection = RfbConnection;
        if (connection == null)
        {
            return;
        }

        if (!connection.DesktopIsResizable || !_autoResize)
        {
            return;
        }

        connection.EnqueueMessage(new SetDesktopSizeMessage((_, currentLayout) => {
            var newSize = new Size((int)(size.Width * Scaling), (int)(size.Height * Scaling));
            var newRectangle = new Rectangle(Position.Origin, newSize);

            Screen newScreen;
            if (!currentLayout.Any())
            {
                // Create a new layout with one screen
                newScreen = new(1, newRectangle, 0);
            }
            else
            {
                // If there is more than one screen, only use one because multi-monitor is not supported
                Screen firstScreen = currentLayout.First();
                newScreen = new(firstScreen.Id, newRectangle, firstScreen.Flags);
            }

            return (newSize, new[] { newScreen }.ToImmutableHashSet());
        }));
    }

    private KeyModifiers CollectModifiers()
    {
        var rc = KeyModifiers.None;
        if (_pressedKeys.Contains(KeySymbol.Control_L) || _pressedKeys.Contains(KeySymbol.Control_R))
        {
            rc |= KeyModifiers.Control;
        }

        if (_pressedKeys.Contains(KeySymbol.Alt_L) || _pressedKeys.Contains(KeySymbol.Alt_R))
        {
            rc |= KeyModifiers.Alt;
        }

        if (_pressedKeys.Contains(KeySymbol.Shift_L) || _pressedKeys.Contains(KeySymbol.Shift_R))
        {
            rc |= KeyModifiers.Shift;
        }

        if (_pressedKeys.Contains(KeySymbol.Meta_L) || _pressedKeys.Contains(KeySymbol.Meta_R))
        {
            rc |= KeyModifiers.Meta;
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
                Bitmap = null;
        }

        _disposed = true;
    }

    private bool HandleKeyEvent(bool downFlag, Key key, KeyModifiers keyModifiers)
    {
        // Get connection
        RfbConnection? connection = RfbConnection;
        if (connection == null)
        {
            return false;
        }

        // Get key symbol
        KeySymbol keySymbol = KeyMapping.GetSymbolFromKey(key, true);
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
        RfbConnection? connection = RfbConnection;
        if (connection == null)
        {
            return false;
        }

        var position = new Position((int)(pointerPoint.X * Scaling), (int)(pointerPoint.Y * Scaling));

        //MouseButtons wheelMask = GetWheelMask(wheelDelta);

        if (wheelDelta != 0)
        {
            MouseButtons wheelMask = wheelDelta > 0 ? MouseButtons.WheelUp : MouseButtons.WheelDown;
            connection.EnqueueMessage(new PointerEventMessage(position, buttonsMask | wheelMask));
        }

        connection.EnqueueMessage(new PointerEventMessage(position, buttonsMask));

        return true;
    }
}
