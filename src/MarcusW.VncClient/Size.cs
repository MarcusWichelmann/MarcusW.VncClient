using System;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Represents a size in device pixels.
    /// </summary>
    /// <remarks>
    /// Based on https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Visuals/Media/PixelSize.cs
    /// </remarks>
    public readonly struct Size : IEquatable<Size>
    {
        /// <summary>
        /// A size representing zero.
        /// </summary>
        public static readonly Size Zero = new Size(0, 0);

        /// <summary>
        /// Gets the width.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the height.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the aspect ratio of the size.
        /// </summary>
        public double AspectRatio => (double)Width / Height;

        /// <summary>
        /// Initializes a new instance of the <see cref="Size"/> structure.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public Size(int width, int height)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;
        }

        /// <summary>
        /// Checks for equality between two <see cref="Size"/>s.
        /// </summary>
        /// <param name="left">The first size.</param>
        /// <param name="right">The second size.</param>
        /// <returns>True if the sizes are equal, otherwise false.</returns>
        public static bool operator ==(Size left, Size right) => left.Equals(right);

        /// <summary>
        /// Checks for inequality between two <see cref="Size"/>s.
        /// </summary>
        /// <param name="left">The first size.</param>
        /// <param name="right">The second size.</param>
        /// <returns>True if the sizes are unequal, otherwise false.</returns>
        public static bool operator !=(Size left, Size right) => !left.Equals(right);

        /// <summary>
        /// Returns a boolean indicating whether the size is equal to the other given size.
        /// </summary>
        /// <param name="other">The other size to test equality against.</param>
        /// <returns>True if this size is equal to other, False otherwise.</returns>
        public bool Equals(Size other) => Width == other.Width && Height == other.Height;

        /// <summary>
        /// Checks for equality between a size and an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        /// True if <paramref name="obj"/> is a size that equals the current size.
        /// </returns>
        public override bool Equals(object? obj) => obj is Size other && Equals(other);

        /// <summary>
        /// Returns a hash code for a <see cref="Size"/>.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Width.GetHashCode();
                hash = hash * 23 + Height.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a new <see cref="Size"/> with the same height and the specified width.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <returns>The new <see cref="Size"/>.</returns>
        public Size WithWidth(int width)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width));

            return new Size(width, Height);
        }

        /// <summary>
        /// Returns a new <see cref="Size"/> with the same width and the specified height.
        /// </summary>
        /// <param name="height">The height.</param>
        /// <returns>The new <see cref="Size"/>.</returns>
        public Size WithHeight(int height)
        {
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            return new Size(Width, height);
        }

        /// <summary>
        /// Returns the string representation of the size.
        /// </summary>
        /// <returns>The string representation of the size.</returns>
        public override string ToString() => $"{Width} x {Height}";
    }
}
