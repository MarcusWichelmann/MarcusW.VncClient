using BenchmarkDotNet.Attributes;

namespace Benchmarks;

public class PropertiesVsFields
{
    private readonly StructWithFields _structWithFields = new(100, 200);
    private readonly StructWithProperties _structWithProperties = new(100, 200);

    [Benchmark]
    public int MultipliedFields() => _structWithFields.A * _structWithFields.B;

    [Benchmark]
    public int MultipliedProperties() => _structWithProperties.A * _structWithProperties.B;

    public readonly struct StructWithProperties(int a, int b)
    {
        public int A { get; } = a;

        public int B { get; } = b;
    }

    public struct StructWithFields(int a, int b)
    {
        public readonly int A = a;

        public readonly int B = b;
    }
}
