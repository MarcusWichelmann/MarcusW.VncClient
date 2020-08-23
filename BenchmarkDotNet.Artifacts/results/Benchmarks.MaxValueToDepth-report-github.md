``` ini

BenchmarkDotNet=v0.12.1, OS=fedora 32
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.106
  [Host]     : .NET Core 3.1.6 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.31603), X64 RyuJIT
  DefaultJob : .NET Core 3.1.6 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.31603), X64 RyuJIT


```
|    Method |      Mean |     Error |    StdDev |
|---------- |----------:|----------:|----------:|
| WhileLoop | 2.5436 ns | 0.0317 ns | 0.0296 ns |
|  PopCount | 0.0000 ns | 0.0000 ns | 0.0000 ns |
