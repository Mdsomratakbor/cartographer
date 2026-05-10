# Cartographer.Mapper v0.2.0 Release Notes

## New Features

- **Mapping Diagnostics & Profiling** — New `MappingDiagnostics` API on `IMapper.Diagnostics` that records per-mapping timing, source/destination types, success/failure, and error messages. Enable via `MapperConfiguration.EnableDiagnostics = true`.
- **Mapping Plan Inspection** — New `IMapper.GetMappingPlans()` method returns a read-only view of all configured type maps and their property mappings for debugging and introspection.
- **.NET 8 Support** — Unit tests now run on .NET 8, 9, and 10. The library already targeted all three; tests now verify all frameworks.

## Bug Fixes

- **Reverse map corrupting convention mappings** — When a forward `MapFrom` expression redirected a source property to a different destination property (e.g., `Conditional` mapped from `Name`), the reverse map configuration would incorrectly overwrite the convention-based `Name → Name` reverse mapping. Fixed by deferring to convention matches over auto-generated reverse source expressions.
- **`MapFrom` generic constraint too restrictive** — `MapFrom<TMember>` required the source expression return type to match the destination member type exactly, preventing mapping between structurally equivalent but differently-named types. Changed to `MapFrom(object?)`.
- **`IncludeBase` generic constraint reversed** — The `where TBaseSource : TSource` constraint required the base type to derive from the derived type, which is logically inverted and also inexpressible in C# method constraints. Removed the constraint entirely.

## Performance

- Benchmarks confirm sub-microsecond mappings for simple, converter, reverse, and inheritance cases.
- `Map_Converter` fastest at ~124 ns with 128 B allocated per operation.
- `Map_Nested` remains the most expensive path due to runtime reflection in `MapValue()` — identified as the primary bottleneck for future optimization.
- Full benchmark results available in `benchmarks/BenchmarkDotNet.Artifacts/results/`.

## Packages

- **Cartographer.Mapper** v0.2.0 — Targets .NET 8.0, .NET 9.0, and .NET 10.0.
