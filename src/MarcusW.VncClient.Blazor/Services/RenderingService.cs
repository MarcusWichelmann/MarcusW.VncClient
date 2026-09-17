using MarcusW.VncClient;
using MarcusW.VncClient.Blazor;

namespace MarcusW.VncClient.Blazor.Services
{
    /// <summary>
    /// Implementation of VNC rendering service
    /// </summary>
    public class RenderingService : IRenderingService
    {
        private readonly JsInterop _jsInterop;
        private byte[]? _cachedRgbaBuffer;
        private bool _disposed = false;

        public RenderingService(JsInterop jsInterop)
        {
            _jsInterop = jsInterop ?? throw new ArgumentNullException(nameof(jsInterop));
        }

        public byte[] ConvertVncDataToRGBA(byte[] vncData, Size size, PixelFormat format)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RenderingService));
            
            int pixelCount = size.Width * size.Height;
            int rgbaSize = pixelCount * 4; // RGBA = 4 bytes per pixel

            // Reuse buffer if possible
            if (_cachedRgbaBuffer == null || _cachedRgbaBuffer.Length != rgbaSize)
            {
                _cachedRgbaBuffer = new byte[rgbaSize];
            }

            int bytesPerPixel = format.BitsPerPixel / 8;

            for (int i = 0; i < pixelCount; i++)
            {
                int vncOffset = i * bytesPerPixel;
                int rgbaOffset = i * 4;

                if (format.BitsPerPixel == 32)
                {
                    // Extract components based on format shifts and masks
                    uint pixel;
                    if (format.BigEndian)
                    {
                        pixel = (uint)((vncData[vncOffset] << 24) |
                                      (vncData[vncOffset + 1] << 16) |
                                      (vncData[vncOffset + 2] << 8) |
                                      vncData[vncOffset + 3]);
                    }
                    else
                    {
                        pixel = (uint)(vncData[vncOffset] |
                                      (vncData[vncOffset + 1] << 8) |
                                      (vncData[vncOffset + 2] << 16) |
                                      (vncData[vncOffset + 3] << 24));
                    }

                    // Extract RGB components
                    uint r = (pixel >> format.RedShift) & ((1u << format.RedMax.GetBitCount()) - 1);
                    uint g = (pixel >> format.GreenShift) & ((1u << format.GreenMax.GetBitCount()) - 1);
                    uint b = (pixel >> format.BlueShift) & ((1u << format.BlueMax.GetBitCount()) - 1);

                    // Scale to 8-bit values
                    _cachedRgbaBuffer[rgbaOffset] = (byte)((r * 255) / format.RedMax);     // R
                    _cachedRgbaBuffer[rgbaOffset + 1] = (byte)((g * 255) / format.GreenMax); // G
                    _cachedRgbaBuffer[rgbaOffset + 2] = (byte)((b * 255) / format.BlueMax);  // B
                    _cachedRgbaBuffer[rgbaOffset + 3] = 255; // A (always opaque)
                }
                else if (format.BitsPerPixel == 16)
                {
                    // Handle 16-bit pixel formats (RGB565, etc.)
                    ushort pixel;
                    if (format.BigEndian)
                    {
                        pixel = (ushort)((vncData[vncOffset] << 8) | vncData[vncOffset + 1]);
                    }
                    else
                    {
                        pixel = (ushort)(vncData[vncOffset] | (vncData[vncOffset + 1] << 8));
                    }

                    uint r = (uint)(pixel >> format.RedShift) & ((1u << format.RedMax.GetBitCount()) - 1);
                    uint g = (uint)(pixel >> format.GreenShift) & ((1u << format.GreenMax.GetBitCount()) - 1);
                    uint b = (uint)(pixel >> format.BlueShift) & ((1u << format.BlueMax.GetBitCount()) - 1);

                    _cachedRgbaBuffer[rgbaOffset] = (byte)((r * 255) / format.RedMax);
                    _cachedRgbaBuffer[rgbaOffset + 1] = (byte)((g * 255) / format.GreenMax);
                    _cachedRgbaBuffer[rgbaOffset + 2] = (byte)((b * 255) / format.BlueMax);
                    _cachedRgbaBuffer[rgbaOffset + 3] = 255;
                }
                else
                {
                    // Fallback for other bit depths
                    _cachedRgbaBuffer[rgbaOffset] = vncData[vncOffset];
                    _cachedRgbaBuffer[rgbaOffset + 1] = vncData[Math.Min(vncOffset + 1, vncData.Length - 1)];
                    _cachedRgbaBuffer[rgbaOffset + 2] = vncData[Math.Min(vncOffset + 2, vncData.Length - 1)];
                    _cachedRgbaBuffer[rgbaOffset + 3] = 255;
                }
            }

            return _cachedRgbaBuffer;
        }

        public async Task RenderFullFramebufferAsync(string canvasId, byte[] framebufferData, Size size, PixelFormat format)
        {
            if (_disposed) return;
            
            try
            {
                byte[] rgbaData = ConvertVncDataToRGBA(framebufferData, size, format);
                await _jsInterop.DrawRectangle(canvasId, rgbaData, 0, 0, size.Width, size.Height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering full framebuffer: {ex.Message}");
            }
        }

        public async Task RenderDirtyRectanglesAsync(string canvasId, byte[] framebufferData, Size framebufferSize, PixelFormat format, IReadOnlyList<Rectangle> dirtyRectangles)
        {
            if (_disposed) return;
            
            try
            {
                foreach (Rectangle rect in dirtyRectangles)
                {
                    byte[] rectangleData = ExtractRectangleData(framebufferData, rect, framebufferSize, format);
                    await RenderRectangleAsync(canvasId, rectangleData, rect, format);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering dirty rectangles: {ex.Message}");
            }
        }

        public async Task RenderRectangleAsync(string canvasId, byte[] rectangleData, Rectangle rect, PixelFormat format)
        {
            if (_disposed) return;
            
            try
            {
                var rectSize = new Size(rect.Size.Width, rect.Size.Height);
                byte[] rgbaData = ConvertVncDataToRGBA(rectangleData, rectSize, format);
                await _jsInterop.DrawRectangle(canvasId, rgbaData, rect.Position.X, rect.Position.Y, rect.Size.Width, rect.Size.Height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering rectangle {rect}: {ex.Message}");
            }
        }

        public byte[] ExtractRectangleData(byte[] framebufferData, Rectangle rect, Size framebufferSize, PixelFormat format)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RenderingService));
            
            int bytesPerPixel = format.BitsPerPixel / 8;
            int rectWidth = rect.Size.Width;
            int rectHeight = rect.Size.Height;
            int framebufferWidth = framebufferSize.Width;

            byte[] rectData = new byte[rectWidth * rectHeight * bytesPerPixel];

            // Copy rectangle data line by line
            for (int y = 0; y < rectHeight; y++)
            {
                int srcOffset = ((rect.Position.Y + y) * framebufferWidth + rect.Position.X) * bytesPerPixel;
                int destOffset = y * rectWidth * bytesPerPixel;
                int lineBytes = rectWidth * bytesPerPixel;

                Array.Copy(framebufferData, srcOffset, rectData, destOffset, lineBytes);
            }

            return rectData;
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            _cachedRgbaBuffer = null;
        }
    }

    /// <summary>
    /// Extension methods for bit counting
    /// </summary>
    internal static class BitCountExtensions
    {
        public static int GetBitCount(this ushort value)
        {
            if (value == 0) return 0;
            
            int count = 0;
            while (value > 0)
            {
                if ((value & 1) == 1) count++;
                value >>= 1;
            }
            return count;
        }
    }
}
