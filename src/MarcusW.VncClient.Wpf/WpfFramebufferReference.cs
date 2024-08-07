using System;
using JetBrains.Annotations;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Wpf;

[PublicAPI]
public class WpfFramebufferReference(int width, int height, IntPtr bitmapBackBuffer, Action? renderDone)
    : IFramebufferReference
{
    private static readonly PixelFormat Rgb24 = new("RGB24", 32, 32, false, true, false, 255, 255, 255, 0, 16, 8, 0, 0);

    public void Dispose()
    {
        renderDone?.Invoke();
    }

    public IntPtr Address { get; } = bitmapBackBuffer;
    public Size Size { get; } = new(width, height);
    public PixelFormat Format { get; } = Rgb24;
    public double HorizontalDpi { get; } = VncView.Dpi;
    public double VerticalDpi { get; } = VncView.Dpi;
}
