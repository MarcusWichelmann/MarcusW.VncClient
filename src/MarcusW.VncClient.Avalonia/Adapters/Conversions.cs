using System.ComponentModel;
using Avalonia;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Avalonia.Adapters
{
    /// <summary>
    /// Helper functions for converting Avalonia specific types to their more abstract pendants.
    /// </summary>
    public static class Conversions
    {
        /// <summary>
        /// Converts a Avalonia PixelSize to a <see cref="Size"/>.
        /// </summary>
        /// <param name="avaloniaPixelSize">Value to convert.</param>
        /// <returns>The conversion result.</returns>
        public static Size GetSize(PixelSize avaloniaPixelSize) => new Size(avaloniaPixelSize.Width, avaloniaPixelSize.Height);

        /// <summary>
        /// Converts a <see cref="Size"/> to a Avalonia PixelSize.
        /// </summary>
        /// <param name="size">Value to convert.</param>
        /// <returns>The conversion result.</returns>
        public static PixelSize GetPixelSize(Size size) => new PixelSize(size.Width, size.Height);

        /// <summary>
        /// Converts a Avalonia PixelFormat to a <see cref="TargetFramebufferFormat"/>.
        /// </summary>
        /// <param name="avaloniaPixelFormat">Value to convert.</param>
        /// <returns>The conversion result.</returns>
        public static TargetFramebufferFormat GetTargetFramebufferFormat(global::Avalonia.Platform.PixelFormat avaloniaPixelFormat)
            => avaloniaPixelFormat switch {
                global::Avalonia.Platform.PixelFormat.Rgb565   => TargetFramebufferFormat.RGB565,
                global::Avalonia.Platform.PixelFormat.Rgba8888 => TargetFramebufferFormat.RGBA8888,
                global::Avalonia.Platform.PixelFormat.Bgra8888 => TargetFramebufferFormat.BGRA8888,
                _                                              => throw new InvalidEnumArgumentException(nameof(avaloniaPixelFormat), (int)avaloniaPixelFormat, typeof(PixelFormat))
            };
    }
}
