using MarcusW.VncClient.Rendering;
using MarcusW.VncClient;
using System.Collections.Immutable;

namespace MarcusW.VncClient.Blazor.Services
{
    /// <summary>
    /// Service for managing VNC framebuffer operations and rendering
    /// </summary>
    public interface IFramebufferService : IDisposable
    {
        /// <summary>
        /// Gets the current framebuffer size
        /// </summary>
        Size FramebufferSize { get; }
        
        /// <summary>
        /// Gets the current pixel format
        /// </summary>
        PixelFormat? PixelFormat { get; }
        
        /// <summary>
        /// Event raised when the framebuffer needs to be updated
        /// </summary>
        event EventHandler<FramebufferUpdateEventArgs>? FramebufferUpdateRequested;
        
        /// <summary>
        /// Event raised when visual invalidation is needed
        /// </summary>
        event EventHandler? VisualInvalidated;
        
        /// <summary>
        /// Initialize or reinitialize the framebuffer with new dimensions and format
        /// </summary>
        void InitializeFramebuffer(Size size, PixelFormat format);
        
        /// <summary>
        /// Get a reference to the framebuffer for writing
        /// </summary>
        IFramebufferReference GrabFramebufferReference(Size size, IImmutableSet<Screen> layout, bool trackChanges = false);
        
        /// <summary>
        /// Queue a framebuffer update for rendering
        /// </summary>
        void QueueFramebufferUpdate(byte[] data, Size size, PixelFormat format, Rectangle? dirtyRect = null);
        
        /// <summary>
        /// Request visual invalidation
        /// </summary>
        void InvalidateVisual();
        
        /// <summary>
        /// Get the current framebuffer data for rendering
        /// </summary>
        byte[]? GetFramebufferData();
        
        /// <summary>
        /// Get dirty rectangles that need rendering
        /// </summary>
        IReadOnlyList<Rectangle> GetDirtyRectangles();
        
        /// <summary>
        /// Clear dirty rectangles after rendering
        /// </summary>
        void ClearDirtyRectangles();
    }
    
    /// <summary>
    /// Event arguments for framebuffer update events
    /// </summary>
    public class FramebufferUpdateEventArgs : EventArgs
    {
        public byte[] Data { get; }
        public Size Size { get; }
        public PixelFormat Format { get; }
        public Rectangle? DirtyRect { get; }
        
        public FramebufferUpdateEventArgs(byte[] data, Size size, PixelFormat format, Rectangle? dirtyRect = null)
        {
            Data = data;
            Size = size;
            Format = format;
            DirtyRect = dirtyRect;
        }
    }
}
