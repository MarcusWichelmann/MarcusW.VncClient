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
        public Rectangle Rectangle { get; }
        public byte BytesPerPixel { get; }

        public int FramebufferLineBytes { get; }

        public int LineWidth { get; }
        public int FirstX { get; }
        public int LastX { get; }
        public int LastY { get; }
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
            Rectangle = rectangle;
            BytesPerPixel = framebufferFormat.BytesPerPixel;

            FramebufferLineBytes = framebufferSize.Width * BytesPerPixel;

            LineWidth = rectangle.Size.Width;
            FirstX = rectangle.Position.X;
            LastX = FirstX + LineWidth - 1;
            LastY = rectangle.Position.Y + rectangle.Size.Height - 1;

            LineBreakBytes = FramebufferLineBytes - LineWidth * BytesPerPixel + BytesPerPixel;

            Debug.Assert(LineBreakBytes > 0, "_lineBreakBytes > 0");

            _currentX = FirstX;
            _currentY = rectangle.Position.Y;
            _positionPtr = framebufferPtr + _currentY * FramebufferLineBytes + _currentX * BytesPerPixel;
        }

        /// <summary>
        /// Gets whether the cursor reached the last pixel of the rectangle.
        /// </summary>
#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        public bool GetEndReached() => _currentY == LastY && _currentX == LastX;

        /// <summary>
        /// Moves the cursor to the next pixel of the rectangle.
        /// </summary>
#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        public void MoveNext()
        {
            // Return to line start when line end is reached
            if (_currentX == LastX)
            {
                // Check if rectangle end is reached
                if (_currentY == LastY)
                    throw new RfbProtocolException("Cannot move the framebuffer cursor beyond the end of the rectangle.");

                _currentX = FirstX;
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
#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        public void MoveForwardInLine(int count)
        {
            // Check for line overflow
            if (_currentX + count > LastX)
                throw new RfbProtocolException($"Moving the framebuffer cursor {count} pixels forward in line would exceed the line end.");

            // Move cursor to the right
            _currentX += count;
            _positionPtr += BytesPerPixel * count;
        }

        /// <summary>
        /// Sets the pixel color on the current position in the rectangle.
        /// </summary>
        /// <param name="color">The desired color.</param>
#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
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
#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        public void SetPixel(byte* pixelData, in PixelFormat pixelFormat)
        {
            PixelConversions.WritePixel(pixelData, pixelFormat, _positionPtr, FramebufferFormat);
        }

        /// <summary>
        /// Sets <see cref="numPixels"/> pixels all to the same color value and advances the cursor accordingly.
        /// </summary>
        /// <param name="color">The desired color.</param>
        /// <param name="numPixels">The number of pixels to set.</param>
#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
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
#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
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

        /// <summary>
        /// Sets <see cref="numPixels"/> pixels and advances the cursor accordingly.
        /// </summary>
        /// <param name="pixelData">A pointer to a memory location where the data for <see cref="numPixels"/> is stored.</param>
        /// <param name="pixelFormat">The format of the pixel data.</param>
        /// <param name="numPixels">The number of pixels to set.</param>
#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        public void SetPixels(byte* pixelData, in PixelFormat pixelFormat, int numPixels)
        {
            // Fast path: When no conversion is necessary, we can copy the data line by line
            if (pixelFormat.IsBinaryCompatibleTo(FramebufferFormat))
            {
                byte* srcPtr = pixelData;
                int pixelsRemaining = numPixels;
                while (pixelsRemaining > 0)
                {
                    // How many more pixels can we fill?
                    int copyPixels = LastX - _currentX + 1;
                    var lineFilled = true;
                    if (pixelsRemaining < copyPixels)
                    {
                        lineFilled = false;
                        copyPixels = pixelsRemaining;
                    }

                    // Fill (the rest of) the line if possible
                    int copyBytes = copyPixels * BytesPerPixel;
                    Unsafe.CopyBlock(_positionPtr, srcPtr, (uint)copyBytes);

                    pixelsRemaining -= copyPixels;
                    srcPtr += copyBytes;

                    // Pixels remaining but last line just filled?
                    if (pixelsRemaining > 0 && _currentY == LastY)
                        throw new RfbProtocolException("There are pixels remaining in the buffer but the cursor already reached the end of the rectangle.");

                    // Advance cursor position
                    if (lineFilled)
                    {
                        _positionPtr += copyBytes - BytesPerPixel + LineBreakBytes;
                        _currentX = FirstX;
                        _currentY++;
                    }
                    else
                    {
                        _positionPtr += copyBytes;
                        _currentX += copyPixels;
                    }
                }

                return;
            }

            // Convert and set the pixels one by one
            byte* pixelPtr = pixelData;
            for (var i = 0; i < numPixels; i++, pixelPtr += BytesPerPixel)
            {
                PixelConversions.WritePixel(pixelPtr, pixelFormat, _positionPtr, FramebufferFormat);
                if (!GetEndReached())
                    MoveNext();
            }
        }

        /// <summary>
        /// Copies all pixels accessible by an other cursor to the current rectangle when both rectangles have the same size.
        /// </summary>
        /// <param name="otherCursor">The other cursor.</param>
        /// <remarks>
        /// Both cursors must point to the start of their rectangle.
        /// The current cursor position is advanced by the written pixels, the other cursor won't be touched.
        /// </remarks>
#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        public void CopyAllFrom(ref FramebufferCursor otherCursor)
        {
            Rectangle otherRectangle = otherCursor.Rectangle;

            if (otherRectangle.Size != Rectangle.Size || otherCursor.BytesPerPixel != BytesPerPixel || otherCursor.FramebufferFormat != FramebufferFormat)
                throw new InvalidOperationException("The other cursor is not equal enough to attempt a full copy.");

            if (_currentX != FirstX || otherCursor._currentX != otherCursor.FirstX || _currentY != Rectangle.Position.Y || otherCursor._currentY != otherRectangle.Position.Y)
                throw new InvalidOperationException("Both cursors must point to the start of their rectangle.");

            // If the rectangles are equal, we have nothing to do
            if (otherCursor._positionPtr == _positionPtr)
                return;

            // When both rectangles overlap and the target rectangle is lower than the source rectangle, we would overwrite the lines we still need to copy later.
            // We can fix this case by starting from the bottom when copying.
            bool startFromBottom = otherRectangle.Overlaps(Rectangle) && Rectangle.Position.Y > otherRectangle.Position.Y;

            // This method will not advance the cursor iteratively as normal, so we work with an offset variable instead.
            var ptrOffset = 0;
            int bytesPerLine = LineWidth * BytesPerPixel;
            int firstY = Rectangle.Position.Y;

            // Prepare for start from bottom
            if (startFromBottom)
            {
                _currentY = LastY;
                ptrOffset = (Rectangle.Size.Height - 1) * FramebufferLineBytes;
            }

            while (true)
            {
                Unsafe.CopyBlock(_positionPtr + ptrOffset, otherCursor._positionPtr + ptrOffset, (uint)bytesPerLine);

                // Go to next line
                if (!startFromBottom && _currentY < LastY)
                {
                    _currentY++;
                    ptrOffset += FramebufferLineBytes;
                }
                else if (startFromBottom && _currentY > firstY)
                {
                    _currentY--;
                    ptrOffset -= FramebufferLineBytes;
                }
                else
                {
                    break;
                }
            }

            // Set the cursor position to the end of the rectangle
            _currentX = LastX;
            _currentY = LastY;
            _positionPtr += Rectangle.Size.Height * bytesPerLine - BytesPerPixel;
        }

        public static bool operator ==(FramebufferCursor left, FramebufferCursor right) => left.Equals(right);

        public static bool operator !=(FramebufferCursor left, FramebufferCursor right) => !left.Equals(right);

        /// <inheritdoc />
        public bool Equals(FramebufferCursor other)
            => FramebufferFormat == other.FramebufferFormat && FirstX == other.FirstX && LastX == other.LastX && LastY == other.LastY && LineBreakBytes == other.LineBreakBytes;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is FramebufferCursor other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(FramebufferFormat, FirstX, LastX, LastY, LineBreakBytes);
    }
}
