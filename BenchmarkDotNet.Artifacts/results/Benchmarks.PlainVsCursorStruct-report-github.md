``` ini

BenchmarkDotNet=v0.12.1, OS=fedora 32
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.105
  [Host]     : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT
  DefaultJob : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT


```
|               Method |     Mean |     Error |    StdDev |
|--------------------- |---------:|----------:|----------:|
|                Plain | 2.790 ms | 0.0086 ms | 0.0081 ms |
|           WithCursor | 2.754 ms | 0.0059 ms | 0.0055 ms |
| WithCursorNotInlined | 8.128 ms | 0.0148 ms | 0.0139 ms |
