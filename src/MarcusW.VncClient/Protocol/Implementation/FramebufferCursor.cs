using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Protocol.Implementation
{
    /// <summary>
    /// Represents a cursor for iterating over pixels of a rectangle inside of a framebuffer.
    /// </summary>
    public unsafe struct FramebufferCursor : IEquatable<FramebufferCursor>
    {
        public PixelFormat FramebufferFormat { get; }
        public byte BytesPerPixel { get; }

        public int LineStart { get; }
        public int LastColumn { get; }
        public int LastLine { get; }
        public int LineBreakBytes { get; }

        private int _currentX;
        private int _currentY;
        private byte* _positionPtr;

        /// <summary>
        /// Initializes a new instance of the <see cref="FramebufferCursor"/> structure.
        /// </summary>
        /// <param name="framebufferReference">The target framebuffer reference.</param>
        /// <param name="rectangle">The target rectangle.</param>
        public FramebufferCursor(IFramebufferReference framebufferReference, in Rectangle rectangle) : this(
            (byte*)(framebufferReference ?? throw new ArgumentNullException(nameof(framebufferReference))).Address, framebufferReference.Format, framebufferReference.Size,
            rectangle) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FramebufferCursor"/> structure.
        /// </summary>
        /// <param name="framebufferPtr">The pointer to the framebuffer location.</param>
        /// <param name="framebufferFormat">The framebuffer format.</param>
        /// <param name="framebufferSize">The framebuffer size.</param>
        /// <param name="rectangle">The target rectangle.</param>
        public FramebufferCursor(byte* framebufferPtr, in PixelFormat framebufferFormat, in Size framebufferSize, in Rectangle rectangle)
        {
            // Rectangle size must not be zero
            if (rectangle.IsEmpty())
                throw new ArgumentException("Given rectangle is empty.", nameof(rectangle));

            // Ensure the rectangle is completely inside the framebuffer to protect against unpredictable behaviour and unsafe memory access.
            if (!rectangle.FitsInside(framebufferSize))
                throw new ArgumentException("Given rectangle lies (partially) outside of the framebuffer area.", nameof(rectangle));

            FramebufferFormat = framebufferFormat;
            BytesPerPixel = framebufferFormat.BytesPerPixel;

            int lineLength = rectangle.Size.Width;
            LineStart = rectangle.Position.X;
            LastColumn = LineStart + lineLength - 1;
            LastLine = rectangle.Position.Y + rectangle.Size.Height - 1;

            int framebufferLineBytes = framebufferSize.Width * BytesPerPixel;
            LineBreakBytes = framebufferLineBytes - lineLength * BytesPerPixel + BytesPerPixel;

            Debug.Assert(LineBreakBytes > 0, "_lineBreakBytes > 0");

            _currentX = LineStart;
            _currentY = rectangle.Position.Y;
            _positionPtr = framebufferPtr + _currentY * framebufferLineBytes + _currentX * BytesPerPixel;
        }

        /// <summary>
        /// Gets whether the cursor reached the last pixel of the rectangle.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool GetEndReached() => _currentY == LastLine && _currentX == LastColumn;

        /// <summary>
        /// Moves the cursor to the next pixel of the rectangle.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void MoveNext()
        {
            // Return to line start when line end is reached
            if (_currentX == LastColumn)
            {
                // Check if rectangle end is reached
                if (_currentY == LastLine)
                    throw new RfbProtocolException("Cannot move the framebuffer cursor beyond the end of the rectangle.");

                _currentX = LineStart;
                _currentY++;
                _positionPtr += LineBreakBytes;
            }
            else
            {
                // Move to next pixel in line
                _currentX++;
                _positionPtr += BytesPerPixel;
            }
        }

        /// <summary>
        /// Moves the cursor a few pixels forward in the current line.
        /// </summary>
        /// <param name="count">The amount of pixels to move the cursor forward.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void MoveForwardInLine(int count)
        {
            // Check for line overflow
            if (_currentX + count > LastColumn)
                throw new RfbProtocolException($"Moving the framebuffer cursor {count} pixels forward in line would exceed the line end.");

            // Move cursor to the right
            _currentX += count;
            _positionPtr += BytesPerPixel * count;
        }

        /// <summary>
        /// Sets the pixel color on the current position in the rectangle.
        /// </summary>
        /// <param name="color">The desired color.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void SetPixel(Color color)
        {
            uint pixelData = color.ToPlainPixel();
            SetPixel((byte*)&pixelData, PixelFormat.Plain);
        }

        /// <summary>
        /// Sets the pixel color on the current position in the rectangle.
        /// </summary>
        /// <param name="pixelData">A pointer to a memory location where the color data is stored.</param>
        /// <param name="pixelFormat">The format of the pixel data.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void SetPixel(byte* pixelData, in PixelFormat pixelFormat)
        {
            PixelConversions.WritePixel(pixelData, pixelFormat, _positionPtr, FramebufferFormat);
        }

        /// <summary>
        /// Sets <see cref="numPixels"/> pixels all to the same color value and advances the cursor accordingly.
        /// </summary>
        /// <param name="color">The desired color.</param>
        /// <param name="numPixels">The number of pixels to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void SetPixelsSolid(Color color, int numPixels)
        {
            uint pixelData = color.ToPlainPixel();
            SetPixelsSolid((byte*)&pixelData, PixelFormat.Plain, numPixels);
        }

        /// <summary>
        /// Sets <see cref="numPixels"/> pixels all to the same color value and advances the cursor accordingly.
        /// </summary>
        /// <param name="pixelData">A pointer to a memory location where the color data is stored.</param>
        /// <param name="pixelFormat">The format of the pixel data.</param>
        /// <param name="numPixels">The number of pixels to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void SetPixelsSolid(byte* pixelData, in PixelFormat pixelFormat, int numPixels)
        {
            // Convert the pixel once
            uint targetPixel;
            PixelConversions.WritePixel(pixelData, pixelFormat, (byte*)&targetPixel, FramebufferFormat);

            // Set all the pixels to the same color
            for (var i = 0; i < numPixels; i++)
            {
                Unsafe.CopyBlock(_positionPtr, (byte*)&targetPixel, BytesPerPixel);
                if (!GetEndReached())
                    MoveNext();
            }
        }

        public static bool operator ==(FramebufferCursor left, FramebufferCursor right) => left.Equals(right);

        public static bool operator !=(FramebufferCursor left, FramebufferCursor right) => !left.Equals(right);

        /// <inheritdoc />
        public bool Equals(FramebufferCursor other)
            => FramebufferFormat == other.FramebufferFormat && LineStart == other.LineStart && LastColumn == other.LastColumn && LastLine == other.LastLine
                && LineBreakBytes == other.LineBreakBytes;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is FramebufferCursor other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(FramebufferFormat, LineStart, LastColumn, LastLine, LineBreakBytes);
    }
}
