using MarcusW.VncClient.Rendering;
using MarcusW.VncClient;
using System.Collections.Immutable;
using MarcusW.VncClient.Blazor.Adapters.Rendering;
using MarcusW.VncClient.Blazor.Extensions;
using Microsoft.Extensions.Options;

namespace MarcusW.VncClient.Blazor.Services
{
    /// <summary>
    /// Implementation of framebuffer management service
    /// </summary>
    public class FramebufferService : IFramebufferService
    {
        private readonly VncClientOptions _options;
        private byte[]? _persistentFramebuffer;
        private Size _framebufferSize = Size.Zero;
        private PixelFormat? _framebufferFormat;
        private readonly List<Rectangle> _dirtyRectangles = new();
        private readonly object _dirtyRectanglesLock = new();
        private bool _disposed = false;

        public Size FramebufferSize => _framebufferSize;
        public PixelFormat? PixelFormat => _framebufferFormat;

        public event EventHandler<FramebufferUpdateEventArgs>? FramebufferUpdateRequested;
        public event EventHandler? VisualInvalidated;

        public FramebufferService(IOptions<VncClientOptions> options)
        {
            _options = options?.Value ?? new VncClientOptions();
        }

        public void InitializeFramebuffer(Size size, PixelFormat format)
        {
            if (_disposed) return;
            
            if (_framebufferSize != size || !_framebufferFormat.Equals(format))
            {
                _framebufferSize = size;
                _framebufferFormat = format;
                
                int bytesPerPixel = format.BitsPerPixel / 8;
                int bufferSize = size.Width * size.Height * bytesPerPixel;
                _persistentFramebuffer = new byte[bufferSize];
                
                lock (_dirtyRectanglesLock)
                {
                    _dirtyRectangles.Clear();
                }
            }
        }

        public IFramebufferReference GrabFramebufferReference(Size size, IImmutableSet<Screen> layout, bool trackChanges = false)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FramebufferService));

            // Initialize framebuffer if needed
            if (_persistentFramebuffer == null || _framebufferSize != size)
            {
                // Use default format if not set
                var format = _framebufferFormat ?? _options.DefaultPixelFormat ?? new PixelFormat(
                    "RGBA", 32, 24, false, true, false,
                    255, 255, 255, 0, 16, 8, 0, 0
                );
                InitializeFramebuffer(size, format);
            }

            // Create a canvas reference for the framebuffer
            var canvasRef = new CanvasReference(
                null, // Will be set by the component
                size,
                _framebufferFormat!.Value,
                _options.DefaultDpi, // Default DPI
                _options.DefaultDpi  // Default DPI
            );

            return new BlazorFramebufferReference(
                canvasRef,
                InvalidateVisual,
                (data, s, fmt, rect) => QueueFramebufferUpdate(data, s, fmt, rect),
                _persistentFramebuffer,
                trackChanges
            );
        }

        public void QueueFramebufferUpdate(byte[] data, Size size, PixelFormat format, Rectangle? dirtyRect = null)
        {
            if (_disposed) return;

            if (dirtyRect.HasValue)
            {
                lock (_dirtyRectanglesLock)
                {
                    _dirtyRectangles.Add(dirtyRect.Value);
                }
            }

            FramebufferUpdateRequested?.Invoke(this, new FramebufferUpdateEventArgs(data, size, format, dirtyRect));
        }

        public void InvalidateVisual()
        {
            if (_disposed) return;
            VisualInvalidated?.Invoke(this, EventArgs.Empty);
        }

        public byte[]? GetFramebufferData()
        {
            return _persistentFramebuffer;
        }

        public IReadOnlyList<Rectangle> GetDirtyRectangles()
        {
            lock (_dirtyRectanglesLock)
            {
                return _dirtyRectangles.ToList();
            }
        }

        public void ClearDirtyRectangles()
        {
            lock (_dirtyRectanglesLock)
            {
                _dirtyRectangles.Clear();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            _persistentFramebuffer = null;
            
            lock (_dirtyRectanglesLock)
            {
                _dirtyRectangles.Clear();
            }
        }
    }
}
