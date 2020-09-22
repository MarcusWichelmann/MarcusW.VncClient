using System.ComponentModel;
using Avalonia;

namespace MarcusW.VncClient.Avalonia
{
    /// <summary>
    /// Helper functions for converting Avalonia specific types to their more abstract pendants.
    /// </summary>
    public static class Conversions
    {
        /// <summary>
        /// Converts a Avalonia <see cref="PixelSize"/> to a <see cref="Size"/>.
        /// </summary>
        /// <param name="avaloniaPixelSize">Value to convert.</param>
        /// <returns>The conversion result.</returns>
        public static Size GetSize(PixelSize avaloniaPixelSize) => new Size(avaloniaPixelSize.Width, avaloniaPixelSize.Height);

        /// <summary>
        /// Converts a <see cref="Size"/> to a Avalonia <see cref="PixelSize"/>.
        /// </summary>
        /// <param name="size">Value to convert.</param>
        /// <returns>The conversion result.</returns>
        public static PixelSize GetPixelSize(Size size) => new PixelSize(size.Width, size.Height);

        /// <summary>
        /// Converts a Avalonia PixelFormat to a <see cref="PixelFormat"/>.
        /// </summary>
        /// <param name="avaloniaPixelFormat">Value to convert.</param>
        /// <returns>The conversion result.</returns>
        public static PixelFormat GetPixelFormat(global::Avalonia.Platform.PixelFormat avaloniaPixelFormat)
            => avaloniaPixelFormat switch {
                // TODO: Actually, the Avalonia PixelFormat is generic and doesn't always refer to Skia. But as long as the pixel representation is identical with Direct2D and others, they can just be renamed.

                // Packed format, component order refers to order in native type (BE): 0xRGB
                global::Avalonia.Platform.PixelFormat.Rgb565 => new PixelFormat("Skia kRGB_565_SkColorType", 16, 16, false, true, false, 31, 63, 31, 0, 11, 5, 0, 0),

                // Array format, component order refers to order in memory (LE): ...RGBA... => 0xABGR
                global::Avalonia.Platform.PixelFormat.Rgba8888 => new PixelFormat("Skia kRGBA_8888_SkColorType", 32, 32, false, true, true, 0xFF, 0xFF, 0xFF, 0xFF, 0, 8, 16, 24),

                // Array format, component order refers to order in memory (LE): ...BGRA... => 0xARGB
                global::Avalonia.Platform.PixelFormat.Bgra8888 => new PixelFormat("Skia kBGRA_8888_SkColorType", 32, 32, false, true, true, 0xFF, 0xFF, 0xFF, 0xFF, 16, 8, 0, 24),

                _ => throw new InvalidEnumArgumentException(nameof(avaloniaPixelFormat), (int)avaloniaPixelFormat, typeof(global::Avalonia.Platform.PixelFormat))
            };

        /// <summary>
        /// Converts a Avalonia <see cref="Point"/> to a <see cref="Position"/>.
        /// </summary>
        /// <param name="point">Value to convert.</param>
        /// <returns>The conversion result.</returns>
        public static Position GetPosition(Point point) => new Position((int)point.X, (int)point.Y);
    }
}
