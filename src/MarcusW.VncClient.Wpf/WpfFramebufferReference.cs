using System;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Wpf;

public class WpfFramebufferReference : IFramebufferReference
{
    private static readonly PixelFormat Rgb24 = new("RGB24", 32, 32, false, true, false, 255, 255, 255, 0, 16, 8, 0, 0);

    private readonly Action _renderDone;

    public WpfFramebufferReference(int width, int height, IntPtr bitmapBackBuffer, Action renderDone)
    {
        _renderDone = renderDone;
        Size = new Size(width, height);
        Format = Rgb24;

        //Format = PixelFormat.Plain;
        Address = bitmapBackBuffer;
        HorizontalDpi = VncView.Dpi;
        VerticalDpi = VncView.Dpi;
    }

    public void Dispose()
    {
        _renderDone?.Invoke();
    }

    public IntPtr Address { get; }
    public Size Size { get; }
    public PixelFormat Format { get; }
    public double HorizontalDpi { get; }
    public double VerticalDpi { get; }
}
