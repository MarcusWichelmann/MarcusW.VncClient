using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class MaxValueToDepth
    {
        private const int MaxValue = 255;

        [Benchmark]
        public uint WhileLoop()
        {
            uint val = MaxValue;
            uint depth = 0;
            while (val != 0)
            {
                depth++;
                val >>= 1;
            }

            return depth;
        }

        [Benchmark]
        public uint PopCount() => Popcnt.PopCount(MaxValue);
    }
}
