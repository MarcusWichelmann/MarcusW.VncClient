using System;

namespace MarcusW.VncClient.Rendering
{
    /// <summary>
    /// A reference to the native framebuffer of the target device. Should be disposed after rendering is finished.
    /// </summary>
    public interface IFramebufferReference : IDisposable
    {
        /// <summary>
        /// Gets the address of the first pixel.
        /// </summary>
        IntPtr Address { get; }

        /// <summary>
        /// Gets the framebuffer size in device pixels.
        /// </summary>
        Size Size { get; }

        /// <summary>
        /// Gets the number of bytes per row.
        /// </summary>
        int RowBytes { get; }

        /// <summary>
        /// Gets the format of how the pixels are stored in memory.
        /// </summary>
        TargetFramebufferFormat Format { get; }

        /// <summary>
        /// Gets the horizontal DPI of the underlying screen.
        /// </summary>
        double HorizontalDpi { get; }

        /// <summary>
        /// Gets the vertical DPI of the underlying screen.
        /// </summary>
        double VerticalDpi { get; }
    }
}
