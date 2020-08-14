``` ini

BenchmarkDotNet=v0.12.1, OS=fedora 32
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.105
  [Host]     : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT
  DefaultJob : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT


```
|          Method |      Mean |     Error |    StdDev |
|---------------- |----------:|----------:|----------:|
|         MemCopy | 0.0036 ns | 0.0041 ns | 0.0039 ns |
| AssigningValues | 1.4780 ns | 0.0056 ns | 0.0047 ns |
