using System;
using System.Runtime.CompilerServices;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Represents a single entry in a VNC color map.
    /// </summary>
    public readonly struct ColorMapEntry : IEquatable<ColorMapEntry>
    {
        /// <summary>
        /// Gets the red component (0-65535).
        /// </summary>
        public ushort Red { get; }

        /// <summary>
        /// Gets the green component (0-65535).
        /// </summary>
        public ushort Green { get; }

        /// <summary>
        /// Gets the blue component (0-65535).
        /// </summary>
        public ushort Blue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorMapEntry"/> structure.
        /// </summary>
        /// <param name="red">The red component (0-65535).</param>
        /// <param name="green">The green component (0-65535).</param>
        /// <param name="blue">The blue component (0-65535).</param>
        public ColorMapEntry(ushort red, ushort green, ushort blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        /// <summary>
        /// Converts the color map entry to an 8-bit RGB color.
        /// </summary>
        /// <returns>The converted color.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color ToColor() => new((byte)(Red >> 8), (byte)(Green >> 8), (byte)(Blue >> 8));

        /// <summary>
        /// Converts the color map entry to a 32-bit RGBA pixel using the specified pixel format.
        /// </summary>
        /// <param name="format">The target pixel format.</param>
        /// <returns>The pixel value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToPixel(in PixelFormat format)
        {
            if (!format.TrueColor)
                throw new InvalidOperationException("Cannot convert color map entry to indexed color format.");

            // Scale components to the format's color depth
            uint r = (uint)((Red * format.RedMax) / 65535);
            uint g = (uint)((Green * format.GreenMax) / 65535);
            uint b = (uint)((Blue * format.BlueMax) / 65535);

            return (r << format.RedShift) | (g << format.GreenShift) | (b << format.BlueShift);
        }

        /// <summary>
        /// Checks for equality between two <see cref="ColorMapEntry"/>s.
        /// </summary>
        /// <param name="other">The other color map entry.</param>
        /// <returns>True if the entries are equal, otherwise false.</returns>
        public bool Equals(ColorMapEntry other) => Red == other.Red && Green == other.Green && Blue == other.Blue;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is ColorMapEntry other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Red, Green, Blue);

        /// <summary>
        /// Checks for equality between two <see cref="ColorMapEntry"/>s.
        /// </summary>
        /// <param name="left">The first entry.</param>
        /// <param name="right">The second entry.</param>
        /// <returns>True if the entries are equal, otherwise false.</returns>
        public static bool operator ==(ColorMapEntry left, ColorMapEntry right) => left.Equals(right);

        /// <summary>
        /// Checks for inequality between two <see cref="ColorMapEntry"/>s.
        /// </summary>
        /// <param name="left">The first entry.</param>
        /// <param name="right">The second entry.</param>
        /// <returns>True if the entries are not equal, otherwise false.</returns>
        public static bool operator !=(ColorMapEntry left, ColorMapEntry right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => $"R:{Red}, G:{Green}, B:{Blue}";
    }
}
