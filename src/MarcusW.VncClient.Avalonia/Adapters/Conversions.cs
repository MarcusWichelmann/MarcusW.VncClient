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
        /// Converts a Avalonia PixelSize to a <see cref="FrameSize"/>.
        /// </summary>
        /// <param name="avaloniaPixelSize">Value to convert.</param>
        /// <returns>The conversion result.</returns>
        public static FrameSize GetFrameSize(PixelSize avaloniaPixelSize)
            => new FrameSize(avaloniaPixelSize.Width, avaloniaPixelSize.Height);

        /// <summary>
        /// Converts a <see cref="FrameSize"/> to a Avalonia PixelSize.
        /// </summary>
        /// <param name="frameSize">Value to convert.</param>
        /// <returns>The conversion result.</returns>
        public static PixelSize GetPixelSize(FrameSize frameSize) => new PixelSize(frameSize.Width, frameSize.Height);

        /// <summary>
        /// Converts a Avalonia PixelFormat to a <see cref="PixelFormat"/>.
        /// </summary>
        /// <param name="avaloniaPixelFormat">Value to convert.</param>
        /// <returns>The conversion result.</returns>
        public static PixelFormat GetPixelFormat(global::Avalonia.Platform.PixelFormat avaloniaPixelFormat)
            => avaloniaPixelFormat switch {
                global::Avalonia.Platform.PixelFormat.Rgb565   => PixelFormat.Rgb565,
                global::Avalonia.Platform.PixelFormat.Rgba8888 => PixelFormat.Rgba8888,
                global::Avalonia.Platform.PixelFormat.Bgra8888 => PixelFormat.Bgra8888,
                _ => throw new InvalidEnumArgumentException(nameof(avaloniaPixelFormat), (int)avaloniaPixelFormat,
                    typeof(global::Avalonia.Platform.PixelFormat))
            };
    }
}
