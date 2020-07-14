using System;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class ImmutableDictionaryLookup
    {
        private const int Index = 500;
        private readonly IImmutableDictionary<int, object> _dictionary= Enumerable.Range(0, 1000).ToImmutableDictionary(i => i, i => new object());

        [Benchmark]
        public object Indexer()
        {
            if (!_dictionary.ContainsKey(Index))
                return null;
            return _dictionary[Index];
        }

        [Benchmark]
        public object TryGet()
        {
            if (!_dictionary.TryGetValue(Index, out object value))
                return null;
            return value;
        }

        [Benchmark]
        public object TryCatch()
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
    }
}
