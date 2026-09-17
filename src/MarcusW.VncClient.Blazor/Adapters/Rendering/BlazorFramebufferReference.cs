using MarcusW.VncClient.Rendering;
using Microsoft.AspNetCore.Components;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MarcusW.VncClient.Blazor.Adapters.Rendering
{
    /// <inheritdoc />
    public sealed class BlazorFramebufferReference : IFramebufferReference
    {
        private CanvasReference? _canvas;
        private readonly Action _invalidateVisual;
        private readonly Action<byte[], Size, PixelFormat, Rectangle?>? _onFramebufferUpdate;
        private readonly byte[] _buffer;
        private readonly IntPtr _bufferPtr;
        private readonly GCHandle _gcHandle;
        private bool _disposed;
        private readonly byte[]? _previousBuffer;
        private readonly bool _trackChanges;

        /// <inheritdoc />
        public IntPtr Address => _disposed ? throw new ObjectDisposedException(nameof(BlazorFramebufferReference)) : _bufferPtr;

        /// <inheritdoc />
        public Size Size => _canvas?.Size ?? throw new ObjectDisposedException(nameof(BlazorFramebufferReference));

        /// <inheritdoc />
        public PixelFormat Format => _canvas?.Format ?? throw new ObjectDisposedException(nameof(BlazorFramebufferReference));

        /// <inheritdoc />
        public double HorizontalDpi => _canvas?.HorizontalDpi ?? throw new ObjectDisposedException(nameof(BlazorFramebufferReference));

        /// <inheritdoc />
        public double VerticalDpi => _canvas?.VerticalDpi ?? throw new ObjectDisposedException(nameof(BlazorFramebufferReference));

        internal BlazorFramebufferReference(CanvasReference canvas, Action invalidateVisual, Action<byte[], Size, PixelFormat, Rectangle?>? onFramebufferUpdate = null, byte[]? persistentBuffer = null, bool trackChanges = false)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _invalidateVisual = invalidateVisual ?? throw new ArgumentNullException(nameof(invalidateVisual));
            _onFramebufferUpdate = onFramebufferUpdate;
            _trackChanges = trackChanges;
            
            // Use provided persistent buffer or create a new one
            if (persistentBuffer != null)
            {
                _buffer = persistentBuffer;
            }
            else
            {
                // Calculate buffer size based on canvas size and pixel format
                int bytesPerPixel = canvas.Format.BitsPerPixel / 8;
                int bufferSize = canvas.Size.Width * canvas.Size.Height * bytesPerPixel;
                _buffer = new byte[bufferSize];
            }
            
            // Create a copy of the buffer for change tracking if enabled
            if (_trackChanges)
            {
                _previousBuffer = new byte[_buffer.Length];
                Array.Copy(_buffer, _previousBuffer, _buffer.Length);
            }
            
            // Pin the buffer and get its address
            _gcHandle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
            _bufferPtr = _gcHandle.AddrOfPinnedObject();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            
            CanvasReference? canvas = _canvas;
            _canvas = null;

            if (canvas != null && _onFramebufferUpdate != null)
            {
                // Notify that an update occurred - the VNC library has already written to our persistent buffer
                try
                {
                    Rectangle? dirtyRect = null;
                    
                    // Detect changed rectangle if tracking is enabled
                    if (_trackChanges && _previousBuffer != null)
                    {
                        dirtyRect = DetectChangedRectangle(canvas.Size, canvas.Format);
                    }
                    
                    // Notify that an update is ready for rendering with optional dirty rectangle
                    _onFramebufferUpdate(_buffer, canvas.Size, canvas.Format, dirtyRect);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing framebuffer update: {ex.Message}");
                }
            }

            // Free the pinned memory
            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }

            // Dispose gets called when rendering is finished, so invalidate the visual now
            _invalidateVisual();
        }
        
        private Rectangle? DetectChangedRectangle(Size size, PixelFormat format)
        {
            if (_previousBuffer == null) return null;
            
            int width = size.Width;
            int height = size.Height;
            int bytesPerPixel = format.BitsPerPixel / 8;
            int stride = width * bytesPerPixel;
            
            // Row-wise comparison using vectorized span operations instead of a per-pixel
            // byte loop. Unchanged rows are skipped with a single SequenceEqual; for changed
            // rows only the first and last differing byte are located to grow the x-range.
            int minY = -1, maxY = -1;
            int minXByte = stride, maxXByteExclusive = 0;
            
            for (int y = 0; y < height; y++)
            {
                ReadOnlySpan<byte> row = new ReadOnlySpan<byte>(_buffer, y * stride, stride);
                ReadOnlySpan<byte> previousRow = new ReadOnlySpan<byte>(_previousBuffer, y * stride, stride);
                
                if (row.SequenceEqual(previousRow))
                    continue;
                
                if (minY < 0) minY = y;
                maxY = y;
                
                int firstDiff = row.CommonPrefixLength(previousRow);
                if (firstDiff < minXByte)
                    minXByte = firstDiff;
                
                // No need to search the tail if the x-range already spans to the row end
                if (maxXByteExclusive < stride)
                {
                    int lastDiff = stride - 1;
                    while (lastDiff > firstDiff && row[lastDiff] == previousRow[lastDiff])
                        lastDiff--;
                    if (lastDiff + 1 > maxXByteExclusive)
                        maxXByteExclusive = lastDiff + 1;
                }
            }
            
            if (minY < 0) return null;
            
            int minX = minXByte / bytesPerPixel;
            int maxX = (maxXByteExclusive - 1) / bytesPerPixel;
            
            // Return the bounding rectangle of all changes
            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
    }
}
