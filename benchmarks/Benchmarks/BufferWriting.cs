using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class BufferWriting
    {
        private readonly byte[] _buffer = new byte[1920 * 1080 * 4];

        [Benchmark]
        public unsafe void UnsafeWithPointers()
        {
            fixed (byte* ptr = &_buffer[0])
            {
                for (int i = 0; i < _buffer.Length; i += 4)
                    SetColor(i, ptr + i);
            }
        }

        [Benchmark]
        public unsafe void UnsafeWithPointersWithoutInlining()
        {
            fixed (byte* ptr = &_buffer[0])
            {
                for (int i = 0; i < _buffer.Length; i += 4)
                    SetColorNotInlined(i, ptr + i);
            }
        }

        [Benchmark]
        public void SafeWithSpans()
        {
            Span<byte> span = _buffer;

            for (int i = 0; i < _buffer.Length; i += 4)
                SetColor(i, span.Slice(i));
        }

        [Benchmark]
        public void SafeWithSpansWithoutInlining()
        {
            Span<byte> span = _buffer;

            for (int i = 0; i < _buffer.Length; i += 4)
                SetColorNotInlined(i, span.Slice(i));
        }

        [Benchmark]
        public void SafeWithSpansWithoutInliningSpanByRef()
        {
            Span<byte> span = _buffer;

            for (int i = 0; i < _buffer.Length; i += 4)
                SetColorNotInlinedSpanByRef(i, span.Slice(i));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void SetColor(int i, byte* position)
        {
            // Some random operations
            *position++ = (byte)(i & 0xff);
            *position++ = (byte)((i >> 8) & 0xff);
            *position++ = (byte)((i >> 16) & 0xff);
            *position = (byte)((i >> 24) & 0xff);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void SetColorNotInlined(int i, byte* position)
        {
            // Some random operations
            *position++ = (byte)(i & 0xff);
            *position++ = (byte)((i >> 8) & 0xff);
            *position++ = (byte)((i >> 16) & 0xff);
            *position = (byte)((i >> 24) & 0xff);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetColor(int i, in Span<byte> position)
        {
            // Some random operations
            position[0] = (byte)(i & 0xff);
            position[1] = (byte)((i >> 8) & 0xff);
            position[2] = (byte)((i >> 16) & 0xff);
            position[3] = (byte)((i >> 24) & 0xff);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetColorNotInlined(int i, Span<byte> position)
        {
            // Some random operations
            position[0] = (byte)(i & 0xff);
            position[1] = (byte)((i >> 8) & 0xff);
            position[2] = (byte)((i >> 16) & 0xff);
            position[3] = (byte)((i >> 24) & 0xff);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetColorNotInlinedSpanByRef(int i, in Span<byte> position)
        {
            // Some random operations
            position[0] = (byte)(i & 0xff);
            position[1] = (byte)((i >> 8) & 0xff);
            position[2] = (byte)((i >> 16) & 0xff);
            position[3] = (byte)((i >> 24) & 0xff);
        }
    }
}
