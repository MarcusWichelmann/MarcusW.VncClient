``` ini

BenchmarkDotNet=v0.12.1, OS=fedora 32
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.105
  [Host]     : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT
  DefaultJob : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT


```
|   Method |     Mean |    Error |   StdDev |
|--------- |---------:|---------:|---------:|
|  Indexer | 55.62 ns | 1.149 ns | 1.018 ns |
|   TryGet | 26.75 ns | 0.405 ns | 0.316 ns |
| TryCatch | 28.29 ns | 0.367 ns | 0.306 ns |
