using System;
using System.Drawing;
using System.Globalization;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Represents a frame size in device pixels.
    /// </summary>
    /// <remarks>
    /// Based on https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Visuals/Media/PixelSize.cs
    /// </remarks>
    public readonly struct FrameSize : IEquatable<FrameSize>
    {
        /// <summary>
        /// A size representing zero.
        /// </summary>
        public static readonly FrameSize Zero = new FrameSize(0, 0);

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
        /// Initializes a new instance of the <see cref="FrameSize"/> structure.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public FrameSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Checks for equality between two <see cref="FrameSize"/>s.
        /// </summary>
        /// <param name="left">The first size.</param>
        /// <param name="right">The second size.</param>
        /// <returns>True if the sizes are equal, otherwise false.</returns>
        public static bool operator ==(FrameSize left, FrameSize right) => left.Equals(right);

        /// <summary>
        /// Checks for inequality between two <see cref="Size"/>s.
        /// </summary>
        /// <param name="left">The first size.</param>
        /// <param name="right">The second size.</param>
        /// <returns>True if the sizes are unequal, otherwise false.</returns>
        public static bool operator !=(FrameSize left, FrameSize right) => !(left == right);

        /// <summary>
        /// Returns a boolean indicating whether the size is equal to the other given size.
        /// </summary>
        /// <param name="other">The other size to test equality against.</param>
        /// <returns>True if this size is equal to other, False otherwise.</returns>
        public bool Equals(FrameSize other) => Width == other.Width && Height == other.Height;

        /// <summary>
        /// Checks for equality between a size and an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        /// True if <paramref name="obj"/> is a size that equals the current size.
        /// </returns>
        public override bool Equals(object? obj) => obj is FrameSize other && Equals(other);

        /// <summary>
        /// Returns a hash code for a <see cref="FrameSize"/>.
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
        /// Returns a new <see cref="FrameSize"/> with the same height and the specified width.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <returns>The new <see cref="FrameSize"/>.</returns>
        public FrameSize WithWidth(int width) => new FrameSize(width, Height);

        /// <summary>
        /// Returns a new <see cref="FrameSize"/> with the same width and the specified height.
        /// </summary>
        /// <param name="height">The height.</param>
        /// <returns>The new <see cref="FrameSize"/>.</returns>
        public FrameSize WithHeight(int height) => new FrameSize(Width, height);

        /// <summary>
        /// Returns the string representation of the size.
        /// </summary>
        /// <returns>The string representation of the size.</returns>
        public override string ToString() => $"{Width}, {Height}";
    }
}
