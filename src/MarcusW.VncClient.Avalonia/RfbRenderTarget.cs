using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using MarcusW.VncClient.Avalonia.Adapters;
using MarcusW.VncClient.Avalonia.Adapters.Rendering;
using MarcusW.VncClient.Rendering;
using IRenderTarget = MarcusW.VncClient.Rendering.IRenderTarget;
using PixelFormat = Avalonia.Platform.PixelFormat;

namespace MarcusW.VncClient.Avalonia
{
    /// <summary>
    /// A control that provides access to a target framebuffer for rendering frames onto it.
    /// </summary>
    public class RfbRenderTarget : Control, IRenderTarget, IDisposable
    {
        private volatile WriteableBitmap? _bitmap;
        private readonly object _bitmapReplacementLock = new object();

        private volatile bool _disposed;

        /// <inheritdoc />
        IFramebufferReference IRenderTarget.GrabFramebufferReference(FrameSize frameSize)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RfbRenderTarget));

            PixelSize requiredPixelSize = Conversions.GetPixelSize(frameSize);

            // Creation of a new buffer necessary?
            // No synchronization necessary, because the only conflicting write operation is
            // in this same method and the field is marked volatile to avoid caching issues.
            // ReSharper disable once InconsistentlySynchronizedField
            bool sizeChanged = _bitmap == null || _bitmap.PixelSize != requiredPixelSize;

            if (sizeChanged)
            {
                // Create new bitmap with required size
                // TODO: BGRA8888 is device-native and much faster?
                // TODO: Detect DPI dynamically
                var newBitmap = new WriteableBitmap(requiredPixelSize, new Vector(96.0f, 96.0f), global::Avalonia.Platform.PixelFormat.Bgra8888);

                // Wait for the rendering being finished before replacing the bitmap
                lock (_bitmapReplacementLock)
                {
                    _bitmap?.Dispose();
                    _bitmap = newBitmap;
                }
            }

            // Lock framebuffer and return as converted reference
            // ReSharper disable once InconsistentlySynchronizedField
            ILockedFramebuffer lockedFramebuffer = _bitmap!.Lock();

            return new AvaloniaFramebufferReference(lockedFramebuffer, () => Dispatcher.UIThread.Post(() => {
                if (sizeChanged)
                    InvalidateMeasure();
                InvalidateVisual();
            }));
        }

        /// <inheritdoc />
        public override void Render(DrawingContext context)
        {
            // Ensure the bitmap does not get disposed or replaced during rendering
            lock (_bitmapReplacementLock)
            {
                if (_bitmap == null)
                    return;

                var rect = new Rect(_bitmap.Size);
                context.DrawImage(_bitmap, 1, rect, rect);
            }
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            // Retrieve bitmap size
            Size? bitmapSize;
            lock (_bitmapReplacementLock)
                bitmapSize = _bitmap?.Size;

            // Has any bitmap been rendered yet?
            if (!bitmapSize.HasValue)
                return Size.Empty;

            // Request the size of the current bitmap
            return bitmapSize.Value;
        }

        /// <inheritdoc />
        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                lock (_bitmapReplacementLock)
                {
                    _bitmap?.Dispose();
                    _bitmap = null;
                }
            }

            _disposed = true;
        }
    }
}
