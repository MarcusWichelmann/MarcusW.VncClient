using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public unsafe class MemoryCopy
    {
        private readonly byte* _srcPtr;
        private readonly byte* _dstPtr;

        public MemoryCopy()
        {
            _srcPtr = (byte*)Marshal.AllocHGlobal(4);
            _dstPtr = (byte*)Marshal.AllocHGlobal(4);
        }

        ~MemoryCopy()
        {
            Marshal.FreeHGlobal((IntPtr)_srcPtr);
            Marshal.FreeHGlobal((IntPtr)_dstPtr);
        }

        [Benchmark]
        public void MemCpy()
        {
            Unsafe.CopyBlock(_dstPtr, _srcPtr, 4);
        }

        [Benchmark]
        public void AssigningValues()
        {
            *_dstPtr = *_srcPtr;
            *(_dstPtr + 1) = *(_srcPtr + 1);
            *(_dstPtr + 2) = *(_srcPtr + 2);
            *(_dstPtr + 3) = *(_srcPtr + 3);
        }

        [Benchmark]
        public void ReinterpretCast()
        {
            uint val = Unsafe.AsRef<uint>(_srcPtr);
            Unsafe.Write(_dstPtr, val);
        }
    }
}
