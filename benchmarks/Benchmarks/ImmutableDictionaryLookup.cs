using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

public class ImmutableDictionaryLookup
{
    private const int Index = 500;

    private readonly IImmutableDictionary<int, object> _dictionary =
        Enumerable.Range(0, 1000).ToImmutableDictionary(i => i, _ => new object());

    [Benchmark]
    public object? Indexer() => CollectionExtensions.GetValueOrDefault(_dictionary, Index);

    [Benchmark]
    public object? TryCatch()
    {
        try
        {
            return _dictionary[Index];
        }
        catch
        {
            return null;
        }
    }

    [Benchmark]
    public object? TryGet() => CollectionExtensions.GetValueOrDefault(_dictionary, Index);
}
