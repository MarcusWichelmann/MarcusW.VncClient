``` ini

BenchmarkDotNet=v0.12.1, OS=fedora 32
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.106
  [Host]     : .NET Core 3.1.6 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.31603), X64 RyuJIT
  DefaultJob : .NET Core 3.1.6 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.31603), X64 RyuJIT


```
|          Method |      Mean |     Error |    StdDev |    Median |
|---------------- |----------:|----------:|----------:|----------:|
|         MemCopy | 0.0011 ns | 0.0044 ns | 0.0039 ns | 0.0000 ns |
| AssigningValues | 1.5015 ns | 0.0134 ns | 0.0118 ns | 1.5015 ns |
| ReinterpretCast | 0.0344 ns | 0.0114 ns | 0.0089 ns | 0.0332 ns |
