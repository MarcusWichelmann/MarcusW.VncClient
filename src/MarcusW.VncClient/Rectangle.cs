using System;
using System.Runtime.CompilerServices;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Represents a rectangle of a given <see cref="Position"/> and <see cref="Size"/>.
    /// </summary>
    public readonly struct Rectangle : IEquatable<Rectangle>
    {
        /// <summary>
        /// A rectangle with the size zero at the origin of the coordinate system.
        /// </summary>
        public static readonly Rectangle Zero = new Rectangle(Position.Origin, Size.Zero);

        /// <summary>
        /// Gets the position of the upper left corner of the rectangle.
        /// </summary>
        public Position Position { get; }

        /// <summary>
        /// Gets the size of the rectangle.
        /// </summary>
        public Size Size { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rectangle"/> structure.
        /// </summary>
        /// <param name="position">The position of the upper left corner of the rectangle.</param>
        /// <param name="size">The size of the rectangle.</param>
        public Rectangle(Position position, Size size)
        {
            Position = position;
            Size = size;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rectangle"/> structure.
        /// </summary>
        /// <param name="x">The x coordinate of the upper left corner of the rectangle.</param>
        /// <param name="y">The y coordinate of the upper left corner of the rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        public Rectangle(int x, int y, int width, int height)
        {
            Position = new Position(x, y);
            Size = new Size(width, height);
        }

        /// <summary>
        /// Checks for equality between two <see cref="Rectangle"/>s.
        /// </summary>
        /// <param name="left">The first rectangle.</param>
        /// <param name="right">The second rectangle.</param>
        /// <returns>True if the rectangles are equal, otherwise false.</returns>
        public static bool operator ==(Rectangle left, Rectangle right) => left.Equals(right);

        /// <summary>
        /// Checks for inequality between two <see cref="Rectangle"/>s.
        /// </summary>
        /// <param name="left">The first rectangle.</param>
        /// <param name="right">The second rectangle.</param>
        /// <returns>True if the rectangles are unequal, otherwise false.</returns>
        public static bool operator !=(Rectangle left, Rectangle right) => !left.Equals(right);

        /// <inheritdoc />
        public bool Equals(Rectangle other) => Position.Equals(other.Position) && Size.Equals(other.Size);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Rectangle other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Position, Size);

        /// <summary>
        /// Returns a new <see cref="Rectangle"/> with the specified position.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>A new rectangle.</returns>
        public Rectangle WithPosition(Position position) => new Rectangle(position, Size);

        /// <summary>
        /// Returns a new <see cref="Rectangle"/> with the specified size.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns>A new rectangle.</returns>
        public Rectangle WithSize(Size size) => new Rectangle(Position, size);

        /// <summary>
        /// Returns whether this rectangle has no content (one side is zero)
        /// </summary>
        /// <returns>True if it has no content, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEmpty() => Size.Width == 0 || Size.Height == 0;

        /// <summary>
        /// Returns whether this rectangle completely fits inside an area in the origin with the given size.
        /// </summary>
        /// <param name="areaSize">The size of the area in which the rectangle should fit in.</param>
        /// <returns>True if it fits inside, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FitsInside(in Size areaSize) => FitsInside(new Rectangle(Position.Origin, areaSize));

        /// <summary>
        /// Returns whether this rectangle completely fits inside the given area.
        /// </summary>
        /// <param name="area">The area in which the rectangle should fit in.</param>
        /// <returns>True if it fits inside, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FitsInside(in Rectangle area)
        {
            static bool InRange(int rectA, int rectB, int areaA, int areaB) => rectA >= areaA && rectB <= areaB;

            return InRange(Position.X, Position.X + Size.Width, area.Position.X, area.Position.X + area.Size.Width)
                && InRange(Position.Y, Position.Y + Size.Height, area.Position.Y, area.Position.Y + area.Size.Height);
        }

        /// <summary>
        /// Returns whether this rectangle overlaps the given area.
        /// </summary>
        /// <param name="area">The area to test for.</param>
        /// <returns>True if they overlap, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(in Rectangle area)
        {
            static bool IsValueInside(int value, int lower, int upper) => value >= lower && value <= upper;

            bool overlapsX = IsValueInside(Position.X, area.Position.X, area.Position.X + area.Size.Width) || IsValueInside(area.Position.X, Position.X, Position.X + Size.Width);
            bool overlapsY = IsValueInside(Position.Y, area.Position.Y, area.Position.Y + area.Size.Height) || IsValueInside(area.Position.Y, Position.Y, Position.Y + Size.Height);

            return overlapsX && overlapsY;
        }

        /// <summary>
        /// Returns a new <see cref="Rectangle"/> that is reduced enough to make it fit inside the given area.
        /// </summary>
        /// <param name="area">The area in which the rectangle should fit in.</param>
        /// <returns>A new rectangle.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Rectangle CroppedTo(in Rectangle area)
        {
            static void Normalize(ref int rectA, ref int rectB, int areaA, int areaB)
            {
                if (rectB <= areaA)
                {
                    rectA = rectB = areaA;
                    return;
                }

                if (rectA >= areaB)
                {
                    rectA = rectB = areaB;
                    return;
                }

                if (rectA < areaA)
                    rectA = areaA;

                if (rectB > areaB)
                    rectB = areaB;
            }

            int xa = Position.X;
            int ya = Position.Y;
            int xb = xa + Size.Width;
            int yb = ya + Size.Height;

            Normalize(ref xa, ref xb, area.Position.X, area.Position.X + area.Size.Width);
            Normalize(ref ya, ref yb, area.Position.Y, area.Position.Y + area.Size.Height);

            return new Rectangle(xa, ya, xb - xa, yb - ya);
        }

        /// <summary>
        /// Returns the string representation of the rectangle.
        /// </summary>
        /// <returns>The string representation of the rectangle.</returns>
        public override string ToString() => $"{Position}, {Size}";
    }
}
