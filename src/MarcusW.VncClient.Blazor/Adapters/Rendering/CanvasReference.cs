using Microsoft.AspNetCore.Components;
using System;

namespace MarcusW.VncClient.Blazor.Adapters.Rendering
{
    public class CanvasReference: IDisposable
    {
        public ElementReference? Canvas { get; private set; }
        public Size Size { get; set; }
        public PixelFormat Format { get; set; }
        public double HorizontalDpi { get; set; }
        public double VerticalDpi { get; set; }
        public string? CanvasId { get; private set; }

        public CanvasReference(ElementReference? canvas, Size size, PixelFormat format, double horizontalDpi, double verticalDpi)
        {
            Canvas = canvas;
            Size = size;
            Format = format;
            HorizontalDpi = horizontalDpi;
            VerticalDpi = verticalDpi;
            
            // Generate a unique canvas ID for JavaScript operations
            CanvasId = canvas?.Id ?? $"canvas_{Guid.NewGuid():N}";
        }
        
        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
