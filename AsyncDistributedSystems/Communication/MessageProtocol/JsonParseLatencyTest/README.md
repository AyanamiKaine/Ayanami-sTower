# How fast we can parse json?

## Notes Regarding MessagePack

- We are using IntKeys, that are faster than string keys
- IntKey: ["foobar", 999]

  - IntKey is always fast in both serialization and deserialization because it does not have to handle and lookup key names, and always has the smaller binary size.

- StringKey: {"name:"foobar","age":999}.

  - StringKey is often a useful, contractless, simple replacement for JSON, interoperability with other languages with MessagePack support, and less error prone versioning. But to achieve maximum performance, use IntKey.

- It seems to better scale as the json(binary) gets larger, in comparison to System.Text.Json

## Results (My System) (From fastest to slowest)

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4602/23H2/2023Update/SunValley3)
13th Gen Intel Core i9-13900K, 1 CPU, 32 logical and 24 physical cores
.NET SDK 9.0.100
[Host] : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2

| Method                             |        Mean |     Error |    StdDev |      Median |   Gen0 |   Gen1 | Allocated |
| ---------------------------------- | ----------: | --------: | --------: | ----------: | -----: | -----: | --------: |
| MessagePack_Simple_Serialize       |    89.32 ns |  6.534 ns |  19.26 ns |    90.92 ns | 0.0025 |      - |      48 B |
| MessagePack_Simple_Deserialize     |   114.25 ns |  7.308 ns |  21.55 ns |   119.89 ns | 0.0063 |      - |     120 B |
| SystemTextJson_Simple_Serialize    |   173.35 ns |  6.829 ns |  20.14 ns |   178.25 ns | 0.0063 |      - |     120 B |
| SystemTextJson_Simple_Deserialize  |   262.76 ns | 13.329 ns |  39.30 ns |   276.82 ns | 0.0098 |      - |     184 B |
| NewtonsoftJson_Simple_Serialize    |   302.78 ns |  6.279 ns |  18.22 ns |   307.10 ns | 0.0761 | 0.0002 |    1432 B |
| MessagePack_Complex_Serialize      |   316.36 ns | 12.221 ns |  35.26 ns |   320.56 ns | 0.0050 |      - |      96 B |
| MessagePack_Complex_Deserialize    |   491.36 ns | 14.850 ns |  43.79 ns |   495.35 ns | 0.0257 |      - |     488 B |
| NewtonsoftJson_Simple_Deserialize  |   514.18 ns | 15.637 ns |  44.10 ns |   525.54 ns | 0.1445 | 0.0010 |    2728 B |
| SystemTextJson_Complex_Serialize   |   711.88 ns | 21.615 ns |  63.73 ns |   711.54 ns | 0.0391 |      - |     736 B |
| NewtonsoftJson_Complex_Serialize   |   933.60 ns | 80.398 ns | 237.05 ns |   793.73 ns | 0.1173 |      - |    2208 B |
| SystemTextJson_Complex_Deserialize | 1,285.04 ns | 44.832 ns | 132.19 ns | 1,307.60 ns | 0.0648 |      - |    1232 B |
| NewtonsoftJson_Complex_Deserialize | 1,971.17 ns | 71.840 ns | 211.82 ns | 2,024.94 ns | 0.1755 |      - |    3328 B |

// _ Warnings _
MultimodalDistribution
JsonParsingBenchmark.MessagePack_Simple_Serialize: Default -> It seems that the distribution can have several modes (mValue = 2.81)
JsonParsingBenchmark.SystemTextJson_Simple_Serialize: Default -> It seems that the distribution is bimodal (mValue = 3.25)
JsonParsingBenchmark.NewtonsoftJson_Complex_Serialize: Default -> It seems that the distribution can have several modes (mValue = 2.83)
JsonParsingBenchmark.SystemTextJson_Complex_Deserialize: Default -> It seems that the distribution can have several modes (mValue = 3.03)

// _ Hints _
Outliers
JsonParsingBenchmark.MessagePack_Simple_Serialize: Default -> 4 outliers were detected (50.32 ns..50.81 ns)
JsonParsingBenchmark.MessagePack_Simple_Deserialize: Default -> 15 outliers were detected (67.47 ns..81.79 ns)
JsonParsingBenchmark.SystemTextJson_Simple_Serialize: Default -> 1 outlier was detected (125.45 ns)
JsonParsingBenchmark.SystemTextJson_Simple_Deserialize: Default -> 12 outliers were detected (149.96 ns..222.20 ns)
JsonParsingBenchmark.NewtonsoftJson_Simple_Serialize: Default -> 3 outliers were removed, 9 outliers were detected (226.89 ns..268.99 ns, 345.36 ns..347.27 ns)
JsonParsingBenchmark.MessagePack_Complex_Serialize: Default -> 4 outliers were removed, 5 outliers were detected (230.53 ns, 405.41 ns..415.83 ns)
JsonParsingBenchmark.MessagePack_Complex_Deserialize: Default -> 4 outliers were detected (335.93 ns..381.17 ns)
JsonParsingBenchmark.NewtonsoftJson_Simple_Deserialize: Default -> 8 outliers were removed, 18 outliers were detected (348.15 ns..443.99 ns, 591.03 ns..615.38 ns)
JsonParsingBenchmark.SystemTextJson_Complex_Serialize: Default -> 2 outliers were detected (502.35 ns, 535.17 ns)
JsonParsingBenchmark.SystemTextJson_Complex_Deserialize: Default -> 1 outlier was detected (875.39 ns)
JsonParsingBenchmark.NewtonsoftJson_Complex_Deserialize: Default -> 5 outliers were detected (1.14 us..1.44 us)

// _ Legends _
Mean : Arithmetic mean of all measurements
Error : Half of 99.9% confidence interval
StdDev : Standard deviation of all measurements
Median : Value separating the higher half of all measurements (50th percentile)
Gen0 : GC Generation 0 collects per 1000 operations
Gen1 : GC Generation 1 collects per 1000 operations
Allocated : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
1 ns : 1 Nanosecond (0.000000001 sec)
