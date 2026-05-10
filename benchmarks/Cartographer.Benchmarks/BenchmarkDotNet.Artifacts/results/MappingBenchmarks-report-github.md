```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6466/22H2/2022Update)
Intel Core i3-7100 CPU 3.90GHz (Kaby Lake), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]     : .NET 8.0.12 (8.0.12, 8.0.1224.60305), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.12 (8.0.12, 8.0.1224.60305), X64 RyuJIT x86-64-v3


```
| Method                            | Mean           | Error        | StdDev        | Median         | Gen0    | Gen1   | Allocated |
|---------------------------------- |---------------:|-------------:|--------------:|---------------:|--------:|-------:|----------:|
| Map_Simple                        |       563.7 ns |     54.30 ns |     160.11 ns |       514.7 ns |  0.2346 |      - |     368 B |
| Map_Simple_Existing               |       441.8 ns |     23.88 ns |      64.57 ns |       431.7 ns |  0.1731 |      - |     272 B |
| Map_Nested                        |     3,410.6 ns |    224.93 ns |     663.20 ns |     3,665.3 ns |  0.9613 |      - |    1513 B |
| Map_Converter                     |       124.4 ns |      7.07 ns |      19.59 ns |       118.5 ns |  0.0815 |      - |     128 B |
| Map_Inheritance                   |       507.1 ns |     13.39 ns |      37.56 ns |       501.0 ns |  0.2594 |      - |     408 B |
| Map_IncludeBase                   |       369.9 ns |      7.36 ns |      19.89 ns |       367.6 ns |  0.1884 |      - |     296 B |
| Map_ReverseMap                    |       114.1 ns |      3.83 ns |      11.05 ns |       110.8 ns |  0.0815 |      - |     128 B |
| Map_PreserveReferences_Cycle      |       459.9 ns |     16.31 ns |      46.53 ns |       449.5 ns |  0.3157 |      - |     496 B |
| Build_Mapper_With_ProfileScanning | 1,257,399.2 ns | 53,862.02 ns | 157,967.98 ns | 1,213,261.3 ns | 19.5313 | 7.8125 |   35661 B |
