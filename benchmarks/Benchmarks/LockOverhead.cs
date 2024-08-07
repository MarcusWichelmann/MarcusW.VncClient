using BenchmarkDotNet.Attributes;

namespace Benchmarks;

public class LockOverhead
{
    private readonly object _lock = new();
    private int _value;

    [Benchmark]
    public void IncreaseWithLock()
    {
        lock (_lock)
            _value++;
    }

    [Benchmark]
    public void IncreaseWithoutLock()
    {
        _value++;
    }
}
