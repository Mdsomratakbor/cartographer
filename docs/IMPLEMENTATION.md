# Cartographer Implementation Overview

This document summarizes the current feature set, how it is implemented, and where to look in the codebase.

## Projects
- `src/Cartographer.Core`: Mapping engine, configuration, and DI extensions.
- `src/Cartographer.App`: Console sample demonstrating basic mapping usage.
- `tests/Cartographer.Tests`: Unit tests covering core behaviors.

## Public API and Core Types
- `Cartographer.Core.Abstractions.IMapper`
  - Runtime mapper. Maps source objects to destination types using compiled delegates.
- `Cartographer.Core.Abstractions.IMapperConfigurationExpression`
  - Configures maps via `CreateMap<TSource, TDestination>()`.
- `Cartographer.Core.Abstractions.ITypeMapExpression` / `IMemberConfigurationExpression`
  - Fluent configuration for `ReverseMap`, `ForMember`, `MapFrom`, and `Ignore`.
- `Cartographer.Core.Configuration.Profile`
  - Base class to group related maps; apply via DI scanning or manual configuration.
- `Cartographer.Core.DependencyInjection.ServiceCollectionExtensions`
  - `AddCartographer(...)` registers configuration and mapper; supports profile scanning across assemblies.

## Configuration Model
- `TypeMap` (`src/Cartographer.Core/Configuration/TypeMap.cs`)
  - Describes a source/destination pair; holds `PropertyMaps` and compiled `MappingFunc`.
- `PropertyMap` (`src/Cartographer.Core/Configuration/PropertyMap.cs`)
  - Describes how a destination property is populated: convention source property, `MapFrom` expression, or ignored.

## Build and Compilation
- `MapperConfiguration` (`src/Cartographer.Core/Configuration/MapperConfiguration.cs`)
  - Accepts configuration actions/profiles, builds `TypeMap` instances, applies convention-based member matching, and compiles delegates.
- `TypeMapExpression` / `MemberConfigurationExpression`
  - Internal fluent builders backing `CreateMap`, `ForMember`, `MapFrom`, `Ignore`, and `ReverseMap`.
- `MapCompiler` (`src/Cartographer.Core/Runtime/MapCompiler.cs`)
  - Generates expression-tree-based delegates per `TypeMap`, supporting nested mapping and collections.

## Runtime Execution
- `SimpleMapper` (`src/Cartographer.Core/Runtime/SimpleMapper.cs`)
  - Executes compiled delegates; throws if a map is missing.

## Behaviors Implemented
- Convention mapping: same-name member matching between source/destination; nested types and collections map recursively.
- Custom member mapping: `ForMember(... MapFrom ...)`, and `Ignore`.
- Reverse mapping: creates the inverse map.
- Performance: expression compilation for each map, avoiding per-call reflection.
- DI integration: `AddCartographer()` registers configuration and mapper; optional profile scanning by assembly.
- Testing: unit tests validate convention mapping, `MapFrom`, `Ignore`, `ReverseMap`, nested mappings, and collection mappings.

## Sample Usage
- `src/Cartographer.App/Program.cs` shows building a mapper with a `UserProfile`, mapping a `User` to `UserDto`, and printing the result.

## Next Feature Ideas (not yet implemented)
- Configuration validation, BeforeMap/AfterMap hooks, conditional mapping, map-into-existing-instance, naming conventions, converters, attributes, global options, inheritance support, open generic maps, projection, and mapping-plan inspection.
