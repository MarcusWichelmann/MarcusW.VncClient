``` ini

BenchmarkDotNet=v0.12.1, OS=fedora 32
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.105
  [Host]     : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT
  DefaultJob : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT


```
|                                Method |      Mean |     Error |    StdDev |
|-------------------------------------- |----------:|----------:|----------:|
|                    UnsafeWithPointers |  2.794 ms | 0.0204 ms | 0.0181 ms |
|     UnsafeWithPointersWithoutInlining |  3.915 ms | 0.0282 ms | 0.0250 ms |
|                         SafeWithSpans |  3.867 ms | 0.0096 ms | 0.0089 ms |
|          SafeWithSpansWithoutInlining | 14.446 ms | 0.0227 ms | 0.0212 ms |
| SafeWithSpansWithoutInliningSpanByRef |  6.242 ms | 0.0118 ms | 0.0105 ms |
