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
        private readonly PixelFormat _framebufferFormat;
        private readonly byte _bytesPerPixel;

        private readonly int _lineStart;
        private readonly int _lastColumn;
        private readonly int _lastLine;
        private readonly int _lineBreakBytes;

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

            _framebufferFormat = framebufferFormat;
            _bytesPerPixel = framebufferFormat.BytesPerPixel;

            int lineLength = rectangle.Size.Width;
            _lineStart = rectangle.Position.X;
            _lastColumn = _lineStart + lineLength - 1;
            _lastLine = rectangle.Position.Y + rectangle.Size.Height - 1;

            int framebufferLineBytes = framebufferSize.Width * _bytesPerPixel;
            _lineBreakBytes = framebufferLineBytes - lineLength * _bytesPerPixel + _bytesPerPixel;

            Debug.Assert(_lineBreakBytes > 0, "_lineBreakBytes > 0");

            _currentX = _lineStart;
            _currentY = rectangle.Position.Y;
            _positionPtr = framebufferPtr + _currentY * framebufferLineBytes + _currentX * _bytesPerPixel;
        }

        /// <summary>
        /// Tries to move the cursor to the next pixel of the rectangle.
        /// </summary>
        /// <returns>True if the cursor was successfully moved, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool TryMoveNext()
        {
            // Return to line start when line end is reached
            if (_currentX == _lastColumn)
            {
                // Check if rectangle end is reached
                if (_currentY == _lastLine)
                    return false;

                _currentX = _lineStart;
                _currentY++;
                _positionPtr += _lineBreakBytes;
            }
            else
            {
                // Move to next pixel in line
                _currentX++;
                _positionPtr += _bytesPerPixel;
            }

            return true;
        }

        /// <summary>
        /// Tries to move the cursor a few pixels forward in the current line.
        /// </summary>
        /// <param name="count">The amount of pixels to move the cursor forward.</param>
        /// <returns>True if the cursor was successfully moved, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool TryMoveForwardInLine(int count)
        {
            // Check for line overflow
            if (_currentX + count >= _lastColumn)
                return false;

            // Move cursor to the right
            _currentX += count;
            _positionPtr += _bytesPerPixel * count;

            return true;
        }

        /// <summary>
        /// Sets the pixel color on the current position in the rectangle.
        /// </summary>
        /// <param name="color">The desired color.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void SetPixel(Color color)
        {
            // Encode the color value using a simple pixel format in memory
            var pixelData = (uint)((color.R << 24) | (color.G << 16) | (color.B << 8) | 255);

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
            PixelConversions.WritePixel(pixelData, pixelFormat, _positionPtr, _framebufferFormat);
        }

        public static bool operator ==(FramebufferCursor left, FramebufferCursor right) => left.Equals(right);

        public static bool operator !=(FramebufferCursor left, FramebufferCursor right) => !left.Equals(right);

        /// <inheritdoc />
        public bool Equals(FramebufferCursor other)
            => _framebufferFormat == other._framebufferFormat && _lineStart == other._lineStart && _lastColumn == other._lastColumn && _lastLine == other._lastLine
                && _lineBreakBytes == other._lineBreakBytes;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is FramebufferCursor other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(_framebufferFormat, _lineStart, _lastColumn, _lastLine, _lineBreakBytes);
    }
}
