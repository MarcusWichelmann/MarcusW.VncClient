using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class PropertiesVsFields
    {
        private StructWithProperties _structWithProperties = new StructWithProperties(100,200);
        private StructWithFields _structWithFields = new StructWithFields(100,200);

        public struct StructWithProperties
        {
            public int A { get; }

            public int B { get; }

            public StructWithProperties(int a, int b)
            {
                A = a;
                B = b;
            }
        }

        public struct StructWithFields
        {
            public int A;

            public int B;

            public StructWithFields(int a, int b)
            {
                A = a;
                B = b;
            }
        }


        [Benchmark]
        public int MultipliedProperties() => _structWithProperties.A * _structWithProperties.B;

        [Benchmark]
        public int MultipliedFields() => _structWithFields.A * _structWithFields.B;
    }
}
