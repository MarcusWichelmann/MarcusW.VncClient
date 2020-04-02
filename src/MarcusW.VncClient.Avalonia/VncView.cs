using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using MarcusW.VncClient.Avalonia.Rendering;
using MarcusW.VncClient.Input;
using MarcusW.VncClient.Rendering;
using PixelFormat = Avalonia.Platform.PixelFormat;

namespace MarcusW.VncClient.Avalonia
{
    /// <summary>
    /// Displays a remote screen using the VNC protocol.
    /// </summary>
    public class VncView : Control, IFramebufferSource, IInputSource, IDisposable
    {
        private WriteableBitmap? _bitmap = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="VncView"/> control.
        /// </summary>
        public VncView() { }

        /// <inheritdoc />
        IFramebufferReference IFramebufferSource.GrabReference(FrameSize frameSize)
        {
            // Creation of a new buffer necessary?
            PixelSize requiredPixelSize = Conversions.GetPixelSize(frameSize);
            if (_bitmap == null || _bitmap.PixelSize != requiredPixelSize)
            {
                // TODO: Race with Render? Memory leak without dispose?
                _bitmap?.Dispose();

                // TODO: Bgra8888 is device-native and much faster?
                // TODO: Detect DPI dynamically
                _bitmap = new WriteableBitmap(requiredPixelSize, new Vector(96.0f, 96.0f), PixelFormat.Bgra8888);
            }

            // Lock framebuffer and return as converted reference
            ILockedFramebuffer lockedFramebuffer = _bitmap.Lock();
            return new AvaloniaFramebufferReference(lockedFramebuffer,
                () => Dispatcher.UIThread.Post(InvalidateVisual));
        }

        /// <inheritdoc />
        public override void Render(DrawingContext context)
        {
            if (_bitmap == null)
                return;

            context.DrawImage(_bitmap,1,new Rect(_bitmap.Size),new Rect(Bounds.Size));
        }

        public void Dispose()
        {
            _bitmap?.Dispose();
            _bitmap = null;
        }
    }
}
