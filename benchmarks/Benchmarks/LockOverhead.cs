using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class LockOverhead
    {
        private int _value;
        private object _lock = new object();

        [Benchmark]
        public void IncreaseWithoutLock()
        {
            _value++;
        }

        [Benchmark]
        public void IncreaseWithLock()
        {
            lock (_lock)
                _value++;
        }
    }
}
