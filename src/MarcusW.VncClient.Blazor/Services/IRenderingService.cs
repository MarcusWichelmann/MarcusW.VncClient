using MarcusW.VncClient;

namespace MarcusW.VncClient.Blazor.Services
{
    /// <summary>
    /// Service for handling VNC rendering operations
    /// </summary>
    public interface IRenderingService : IDisposable
    {
        /// <summary>
        /// Convert VNC pixel data to RGBA format for web rendering
        /// </summary>
        byte[] ConvertVncDataToRGBA(byte[] vncData, Size size, PixelFormat format);
        
        /// <summary>
        /// Render the full framebuffer to canvas
        /// </summary>
        Task RenderFullFramebufferAsync(string canvasId, byte[] framebufferData, Size size, PixelFormat format);
        
        /// <summary>
        /// Render only dirty rectangles to canvas
        /// </summary>
        Task RenderDirtyRectanglesAsync(string canvasId, byte[] framebufferData, Size framebufferSize, PixelFormat format, IReadOnlyList<Rectangle> dirtyRectangles);
        
        /// <summary>
        /// Render a specific rectangle to canvas
        /// </summary>
        Task RenderRectangleAsync(string canvasId, byte[] rectangleData, Rectangle rect, PixelFormat format);
        
        /// <summary>
        /// Extract rectangle data from full framebuffer
        /// </summary>
        byte[] ExtractRectangleData(byte[] framebufferData, Rectangle rect, Size framebufferSize, PixelFormat format);
    }
}
