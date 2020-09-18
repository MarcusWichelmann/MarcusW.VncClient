using System;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Represents a single RGB color value.
    /// </summary>
    public readonly struct Color : IEquatable<Color>
    {
        /// <summary>
        /// Gets the red value.
        /// </summary>
        public byte R { get; }

        /// <summary>
        /// Gets the green value.
        /// </summary>
        public byte G { get; }

        /// <summary>
        /// Gets the blue value.
        /// </summary>
        public byte B { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color"/> structure.
        /// </summary>
        /// <param name="r">The red value.</param>
        /// <param name="g">The green value.</param>
        /// <param name="b">The blue value.</param>
        public Color(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// Checks for equality between two <see cref="Color"/>s.
        /// </summary>
        /// <param name="left">The first color.</param>
        /// <param name="right">The second color.</param>
        /// <returns>True if the colors are equal, otherwise false.</returns>
        public static bool operator ==(Color left, Color right) => left.Equals(right);

        /// <summary>
        /// Checks for inequality between two <see cref="Color"/>s.
        /// </summary>
        /// <param name="left">The first color.</param>
        /// <param name="right">The second color.</param>
        /// <returns>True if the colors are unequal, otherwise false.</returns>
        public static bool operator !=(Color left, Color right) => !left.Equals(right);

        /// <inheritdoc />
        public bool Equals(Color other) => R == other.R && G == other.G && B == other.B;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Color other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(R, G, B);

        /// <summary>
        /// Returns the string representation of the color.
        /// </summary>
        /// <returns>The string representation of the color.</returns>
        public override string ToString() => $"RGB({R},{G},{B})";
    }
}
