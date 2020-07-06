using System;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// Represents a pixel format that's used for RFB encodings.
    /// </summary>
    public readonly struct RfbPixelFormat : IEquatable<RfbPixelFormat>
    {
        /// <summary>
        /// Gets the number of bits used for each pixel on the wire.
        /// </summary>
        public byte BitsPerPixel { get; }

        /// <summary>
        /// Gets the number of useful bits in the pixel value. Must be greater than or equal to <see cref="BitsPerPixel"/>.
        /// </summary>
        public byte Depth { get; }

        /// <summary>
        /// Gets if multi-byte pixels are interpreted as big endian.
        /// </summary>
        public bool BigEndian { get; }

        /// <summary>
        /// Gets whether the pixel value is composed from the color values (True), or if the color values serve as indices into a color map (False).
        /// </summary>
        public bool TrueColor { get; }

        /// <summary>
        /// Gets the maximum value for the color red.
        /// </summary>
        public ushort RedMax { get; }

        /// <summary>
        /// Gets the maximum value for the color green.
        /// </summary>
        public ushort GreenMax { get; }

        /// <summary>
        /// Gets the maximum value for the color blue.
        /// </summary>
        public ushort BlueMax { get; }

        /// <summary>
        /// Gets the number of right-shifts needed to get the red value in a pixel.
        /// </summary>
        public byte RedShift { get; }

        /// <summary>
        /// Gets the number of right-shifts needed to get the green value in a pixel.
        /// </summary>
        public byte GreenShift { get; }

        /// <summary>
        /// Gets the number of right-shifts needed to get the blue value in a pixel.
        /// </summary>
        public byte BlueShift { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RfbPixelFormat"/> structure.
        /// </summary>
        /// <param name="bitsPerPixel">The number of bits used for each pixel on the wire.</param>
        /// <param name="depth">The number of useful bits in the pixel value. Must be greater than or equal to <paramref name="bitsPerPixel"/>.</param>
        /// <param name="bigEndian">True if multi-byte pixels are interpreted as big endian, otherwise false.</param>
        /// <param name="trueColor">True if the pixel value is composed from the color values, or false if the color values serve as indices into a color map.</param>
        /// <param name="redMax">The maximum value for the color red.</param>
        /// <param name="greenMax">The maximum value for the color green.</param>
        /// <param name="blueMax">The maximum value for the color blue.</param>
        /// <param name="redShift">The number of right-shifts needed to get the red value in a pixel.</param>
        /// <param name="greenShift">The number of right-shifts needed to get the green value in a pixel.</param>
        /// <param name="blueShift">The number of right-shifts needed to get the blue value in a pixel.</param>
        public RfbPixelFormat(byte bitsPerPixel, byte depth, bool bigEndian, bool trueColor, ushort redMax, ushort greenMax, ushort blueMax, byte redShift, byte greenShift,
            byte blueShift)
        {
            BitsPerPixel = bitsPerPixel;
            Depth = depth;
            BigEndian = bigEndian;
            TrueColor = trueColor;
            RedMax = redMax;
            GreenMax = greenMax;
            BlueMax = blueMax;
            RedShift = redShift;
            GreenShift = greenShift;
            BlueShift = blueShift;
        }

        /// <summary>
        /// Checks for equality between two <see cref="RfbPixelFormat"/>s.
        /// </summary>
        /// <param name="left">The first pixel format.</param>
        /// <param name="right">The second pixel format.</param>
        /// <returns>True if the sizes are equal, otherwise false.</returns>
        public static bool operator ==(RfbPixelFormat left, RfbPixelFormat right) => left.Equals(right);

        /// <summary>
        /// Checks for inequality between two <see cref="RfbPixelFormat"/>s.
        /// </summary>
        /// <param name="left">The first pixel format.</param>
        /// <param name="right">The second pixel format.</param>
        /// <returns>True if the sizes are unequal, otherwise false.</returns>
        public static bool operator !=(RfbPixelFormat left, RfbPixelFormat right) => !(left == right);

        /// <inheritdoc />
        public bool Equals(RfbPixelFormat other)
            => BitsPerPixel == other.BitsPerPixel && Depth == other.Depth && BigEndian == other.BigEndian && TrueColor == other.TrueColor && RedMax == other.RedMax
                && GreenMax == other.GreenMax && BlueMax == other.BlueMax && RedShift == other.RedShift && GreenShift == other.GreenShift && BlueShift == other.BlueShift;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is RfbPixelFormat other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(BitsPerPixel);
            hashCode.Add(Depth);
            hashCode.Add(BigEndian);
            hashCode.Add(TrueColor);
            hashCode.Add(RedMax);
            hashCode.Add(GreenMax);
            hashCode.Add(BlueMax);
            hashCode.Add(RedShift);
            hashCode.Add(GreenShift);
            hashCode.Add(BlueShift);
            return hashCode.ToHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
            => $"Depth {Depth} ({BitsPerPixel}bpp), {(BigEndian ? "Big endian" : "Little endian")} {(TrueColor ? "true" : "mapped")} RGB ({RedMax} {GreenMax} {BlueMax} shift {RedShift} {GreenShift} {BlueShift})";

        /// <summary>
        /// Tries to find a matching <see cref="FrameFormat"/> for this pixel format.
        /// </summary>
        /// <returns>A matching <see cref="FrameFormat"/>.</returns>
        public FrameFormat AsFrameFormat()
        {
            if (RedShift > GreenShift && GreenShift > BlueShift)
            {
                if (Depth == 16)
                    return FrameFormat.RGB565;
                if (Depth == 24)
                    return FrameFormat.RGB888;
                if (Depth == 32)
                    return FrameFormat.RGBA8888;
            }
            else if (BlueShift > GreenShift && GreenShift > RedShift)
            {
                if (Depth == 16)
                    return FrameFormat.BGR565;
                if (Depth == 24)
                    return FrameFormat.BGR888;
                if (Depth == 32)
                    return FrameFormat.BGRA8888;
            }

            throw new UnexpectedDataException($"The pixel format does not match any known format type: {this}");
        }
    }
}
