using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class BufferWriting
    {
        private readonly byte[] _buffer = new byte[1920 * 1080 * 4];

        [Benchmark]
        public void ArrayIndexer()
        {
            for (int i = 0; i < _buffer.Length; i += 4)
                SetPixelArrayIndexer(_buffer, i, 0xffffffff);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetPixelArrayIndexer(byte[] buffer, int i, uint color)
        {
            buffer[i] = (byte)(color & 0xff);
            buffer[i + 1] = (byte)((color >> 8) & 0xff);
            buffer[i + 2] = (byte)((color >> 16) & 0xff);
            buffer[i + 3] = (byte)((color >> 24) & 0xff);
        }

        [Benchmark]
        public void Span()
        {
            Span<byte> buffer = _buffer;

            for (int i = 0; i < _buffer.Length; i += 4)
                SetPixelSpan(buffer.Slice(i, 4), 0xffffffff);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetPixelSpan(in Span<byte> span, uint color)
        {
            span[0] = (byte)(color & 0xff);
            span[1] = (byte)((color >> 8) & 0xff);
            span[2] = (byte)((color >> 16) & 0xff);
            span[3] = (byte)((color >> 24) & 0xff);
        }

        [Benchmark]
        public unsafe void Pointer()
        {
            fixed (byte* ptr = &_buffer[0])
            {
                for (int i = 0; i < _buffer.Length; i += 4)
                    SetPixelPointer(ptr + i, 0xffffffff);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void SetPixelPointer(byte* ptr, uint color)
        {
            *ptr++ = (byte)(color & 0xff);
            *ptr++ = (byte)((color >> 8) & 0xff);
            *ptr++ = (byte)((color >> 16) & 0xff);
            *ptr = (byte)((color >> 24) & 0xff);
        }

        [Benchmark]
        public unsafe void PointerReinterpretCast()
        {
            fixed (byte* ptr = &_buffer[0])
            {
                for (int i = 0; i < _buffer.Length; i += 4)
                    SetPixelPointerReinterpretCast(ptr + i, 0xffffffff);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void SetPixelPointerReinterpretCast(byte* ptr, uint color)
        {
            *(uint*)ptr = color;
        }

        [Benchmark]
        public unsafe void PointerMemcopy()
        {
            fixed (byte* ptr = &_buffer[0])
            {
                for (int i = 0; i < _buffer.Length; i += 4)
                    SetPixelPointerMemcopy(ptr + i, 0xffffffff);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void SetPixelPointerMemcopy(byte* ptr, uint color)
        {
            Unsafe.CopyBlock(ptr, &color, sizeof(uint));
        }
    }
}
