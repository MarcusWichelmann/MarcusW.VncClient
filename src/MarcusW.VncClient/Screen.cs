using System;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Represents a screen as part of a remote framebuffer.
    /// </summary>
    public readonly struct Screen : IEquatable<Screen>
    {
        /// <summary>
        /// Gets the screen id.
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// Gets the rectangle of the screen on the framebuffer.
        /// </summary>
        public Rectangle Rectangle { get; }

        /// <summary>
        /// Gets the flags.
        /// </summary>
        public uint Flags { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Screen"/> structure.
        /// </summary>
        /// <param name="id">The screen id.</param>
        /// <param name="position">The rectangle of the screen on the framebuffer.</param>
        /// <param name="flags">The flags.</param>
        public Screen(uint id, Rectangle rectangle, uint flags)
        {
            Id = id;
            Rectangle = rectangle;
            Flags = flags;
        }

        /// <summary>
        /// Checks for equality between two <see cref="Screen"/>s.
        /// </summary>
        /// <param name="left">The first screen.</param>
        /// <param name="right">The second screen.</param>
        /// <returns>True if the screens are equal, otherwise false.</returns>
        public static bool operator ==(Screen left, Screen right) => left.Equals(right);

        /// <summary>
        /// Checks for inequality between two <see cref="Screen"/>s.
        /// </summary>
        /// <param name="left">The first screen.</param>
        /// <param name="right">The second screen.</param>
        /// <returns>True if the screens are unequal, otherwise false.</returns>
        public static bool operator !=(Screen left, Screen right) => !left.Equals(right);

        /// <inheritdoc />
        public bool Equals(Screen other) => Id == other.Id && Rectangle.Equals(other.Rectangle) && Flags == other.Flags;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Screen other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Id, Rectangle, Flags);

        /// <summary>
        /// Returns the string representation of the screen.
        /// </summary>
        /// <returns>The string representation of the screen.</returns>
        public override string ToString() => $"Screen {Id}({Flags}): {Rectangle}";
    }
}
