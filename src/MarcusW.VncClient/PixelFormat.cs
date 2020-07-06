using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Represents a pixel format that's used for RFB encodings.
    /// </summary>
    public readonly struct PixelFormat : IEquatable<PixelFormat>
    {
        /// <summary>
        /// An invalid pixel format representing unknown values.
        /// </summary>
        public static readonly PixelFormat Unknown = new PixelFormat(0, 0, true, true, 0, 0, 0, 0, 0, 0);

        // ReSharper disable InconsistentNaming

        public static readonly PixelFormat RGB111 = new PixelFormat(8, 3, !BitConverter.IsLittleEndian, true, 1, 1, 1, 2, 1, 0);
        public static readonly PixelFormat BGR111 = new PixelFormat(8, 3, !BitConverter.IsLittleEndian, true, 1, 1, 1, 0, 1, 2);

        public static readonly PixelFormat RGB222 = new PixelFormat(8, 6, !BitConverter.IsLittleEndian, true, 3, 3, 3, 4, 2, 0);
        public static readonly PixelFormat BGR222 = new PixelFormat(8, 6, !BitConverter.IsLittleEndian, true, 3, 3, 3, 0, 2, 4);

        public static readonly PixelFormat RGB332 = new PixelFormat(8, 8, !BitConverter.IsLittleEndian, true, 7, 7, 3, 5, 2, 0);
        public static readonly PixelFormat BGR332 = new PixelFormat(8, 8, !BitConverter.IsLittleEndian, true, 3, 7, 7, 0, 2, 5);

        public static readonly PixelFormat RGB565 = new PixelFormat(16, 16, !BitConverter.IsLittleEndian, true, 31, 63, 31, 11, 5, 0);
        public static readonly PixelFormat BGR565 = new PixelFormat(16, 16, !BitConverter.IsLittleEndian, true, 31, 63, 31, 0, 5, 11);

        public static readonly PixelFormat RGB888 = new PixelFormat(32, 24, !BitConverter.IsLittleEndian, true, 0xFF, 0xFF, 0xFF, 16, 8, 0);
        public static readonly PixelFormat BGR888 = new PixelFormat(32, 24, !BitConverter.IsLittleEndian, true, 0xFF, 0xFF, 0xFF, 0, 8, 16);

        // ReSharper restore InconsistentNaming

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
        /// Initializes a new instance of the <see cref="PixelFormat"/> structure.
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
        public PixelFormat(byte bitsPerPixel, byte depth, bool bigEndian, bool trueColor, ushort redMax, ushort greenMax, ushort blueMax, byte redShift, byte greenShift,
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
        /// Checks for equality between two <see cref="PixelFormat"/>s.
        /// </summary>
        /// <param name="left">The first pixel format.</param>
        /// <param name="right">The second pixel format.</param>
        /// <returns>True if the sizes are equal, otherwise false.</returns>
        public static bool operator ==(PixelFormat left, PixelFormat right) => left.Equals(right);

        /// <summary>
        /// Checks for inequality between two <see cref="PixelFormat"/>s.
        /// </summary>
        /// <param name="left">The first pixel format.</param>
        /// <param name="right">The second pixel format.</param>
        /// <returns>True if the sizes are unequal, otherwise false.</returns>
        public static bool operator !=(PixelFormat left, PixelFormat right) => !(left == right);

        /// <inheritdoc />
        public bool Equals(PixelFormat other)
            => BitsPerPixel == other.BitsPerPixel && Depth == other.Depth && BigEndian == other.BigEndian && TrueColor == other.TrueColor && RedMax == other.RedMax
                && GreenMax == other.GreenMax && BlueMax == other.BlueMax && RedShift == other.RedShift && GreenShift == other.GreenShift && BlueShift == other.BlueShift;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is PixelFormat other && Equals(other);

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
            => this == Unknown
                ? "Unknown"
                : $"Depth {Depth} ({BitsPerPixel}bpp), {(BigEndian ? "BE" : "LE")} {(TrueColor ? "true-color" : "mapped")} RGB ({RedMax} {GreenMax} {BlueMax} shift {RedShift} {GreenShift} {BlueShift})";

        /// <summary>
        /// Gets a short string representation of this format (like RGB888).
        /// </summary>
        /// <returns>A short string.</returns>
        public string ToShortString()
        {
            // Based on https://github.com/TigerVNC/tigervnc/blob/8c6c584377feba0e3b99eecb3ef33b28cee318cb/common/rfb/PixelFormat.cxx#L513

            // Is the color in RGB order?
            if (BlueShift == 0 && RedShift > GreenShift && GreenShift > BlueShift && BlueMax == (1 << GreenShift) - 1 && GreenMax == (1 << (RedShift - GreenShift)) - 1
                && RedMax == (1 << (Depth - RedShift)) - 1)
                return $"RGB{Depth - RedShift}{RedShift - GreenShift}{GreenShift}";

            // Is the color in BGR order?
            if (RedShift == 0 && BlueShift > GreenShift && GreenShift > RedShift && RedMax == (1 << GreenShift) - 1 && GreenMax == (1 << (BlueShift - GreenShift)) - 1
                && BlueMax == (1 << (Depth - BlueShift)) - 1)
                return $"BGR{Depth - BlueShift}{BlueShift - GreenShift}{GreenShift}";

            throw new UnexpectedDataException("Pixel format data is of an unknown order.");
        }
    }
}
