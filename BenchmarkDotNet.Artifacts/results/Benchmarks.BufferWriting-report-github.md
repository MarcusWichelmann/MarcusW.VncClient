``` ini

BenchmarkDotNet=v0.12.1, OS=fedora 32
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.109
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT


```
|                 Method |       Mean |    Error |   StdDev |
|----------------------- |-----------:|---------:|---------:|
|           ArrayIndexer | 2,656.1 μs |  7.89 μs |  7.00 μs |
|                   Span | 2,468.1 μs |  5.70 μs |  5.06 μs |
|                Pointer | 2,788.5 μs | 14.76 μs | 13.08 μs |
| PointerReinterpretCast | 1,122.1 μs |  4.44 μs |  3.94 μs |
|         PointerMemcopy |   948.7 μs | 14.25 μs | 21.76 μs |
