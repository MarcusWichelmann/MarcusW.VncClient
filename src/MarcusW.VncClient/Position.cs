using System;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Represents a position in device pixels.
    /// </summary>
    public readonly struct Position : IEquatable<Position>
    {
        /// <summary>
        /// A position representing the origin of a coordinate system.
        /// </summary>
        public static readonly Position Origin = new Position(0, 0);

        /// <summary>
        /// Gets the X coordinate.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Gets the Y coordinate.
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Position"/> structure.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Checks for equality between two <see cref="Position"/>s.
        /// </summary>
        /// <param name="left">The first position.</param>
        /// <param name="right">The second position.</param>
        /// <returns>True if the positions are equal, otherwise false.</returns>
        public static bool operator ==(Position left, Position right) => left.Equals(right);

        /// <summary>
        /// Checks for inequality between two <see cref="Position"/>s.
        /// </summary>
        /// <param name="left">The first position.</param>
        /// <param name="right">The second position.</param>
        /// <returns>True if the positions are unequal, otherwise false.</returns>
        public static bool operator !=(Position left, Position right) => !left.Equals(right);

        /// <inheritdoc />
        public bool Equals(Position other) => X == other.X && Y == other.Y;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Position other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(X, Y);

        /// <summary>
        /// Returns the string representation of the position.
        /// </summary>
        /// <returns>The string representation of the position.</returns>
        public override string ToString() => $"{X}, {Y}";
    }
}
