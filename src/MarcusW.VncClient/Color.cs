using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace MarcusW.VncClient;

/// <summary>
///     Represents a single RGB color value.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="Color" /> structure.
/// </remarks>
/// <param name="r">The red value.</param>
/// <param name="g">The green value.</param>
/// <param name="b">The blue value.</param>
[PublicAPI]
public readonly struct Color(byte r, byte g, byte b) : IEquatable<Color>
{
    /// <summary>
    ///     Gets the red value.
    /// </summary>
    public byte R { get; } = r;

    /// <summary>
    ///     Gets the green value.
    /// </summary>
    public byte G { get; } = g;

    /// <summary>
    ///     Gets the blue value.
    /// </summary>
    public byte B { get; } = b;

    /// <summary>
    ///     Returns the pixel data for this color using the <see cref="PixelFormat.Plain" /> encoding.
    /// </summary>
    /// <returns>The pixel data.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ToPlainPixel() => (uint)((R << 24) | (G << 16) | (B << 8) | 255);

    /// <summary>
    ///     Checks for equality between two <see cref="Color" />s.
    /// </summary>
    /// <param name="left">The first color.</param>
    /// <param name="right">The second color.</param>
    /// <returns>True if the colors are equal, otherwise false.</returns>
    public static bool operator ==(Color left, Color right) => left.Equals(right);

    /// <summary>
    ///     Checks for inequality between two <see cref="Color" />s.
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
    ///     Returns the string representation of the color.
    /// </summary>
    /// <returns>The string representation of the color.</returns>
    public override string ToString() => $"RGB({R},{G},{B})";
}
