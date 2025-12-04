# Cartographer Implementation Overview

This document summarizes the current feature set, how it is implemented, and where to look in the codebase.

## Projects
- `src/Cartographer.Core`: Mapping engine, configuration, DI extensions.
- `src/Cartographer.App`: Console sample showing hooks, conditional mapping, map-into-existing-instance, naming conventions.
- `example/Cartographer.Example.Api`: .NET 9 Web API showcasing profiles, DI integration, inheritance (`Include`), global options, and in-memory services.
- `example/Cartographer.Example.Net8Api`: .NET 8 Web API mirroring the feature set to verify multi-version support.
- `tests/Cartographer.Tests`: Unit tests covering mapping behavior, validation, converters, inheritance, collections, etc.

## Public API and Core Types
- `Cartographer.Core.Abstractions.IMapper`
  - Runtime mapper. Maps source objects to destination types using compiled delegates.
- `Cartographer.Core.Abstractions.IMapperConfigurationExpression`
  - Configures maps via `CreateMap<TSource, TDestination>()`.
- `Cartographer.Core.Abstractions.ITypeMapExpression` / `IMemberConfigurationExpression`
  - Fluent configuration for `ReverseMap`, `ForMember`, `BeforeMap`, `AfterMap`, `MapFrom`, `Ignore`, `Condition`, `PreCondition`, `ConvertUsing`, `Include`, and `IncludeBase`.
- `Cartographer.Core.Configuration.Profile`
  - Base class to group related maps; apply via DI scanning or manual configuration.
- `Cartographer.Core.DependencyInjection.ServiceCollectionExtensions`
  - `AddCartographer(...)` registers configuration and mapper; supports profile scanning across assemblies.

## Configuration Model
- `TypeMap` (`src/Cartographer.Core/Configuration/TypeMap.cs`)
  - Describes a source/destination pair; holds `PropertyMaps`, compiled delegates, and derived map metadata (`Include`/`IncludeBase`).
- `PropertyMap` (`src/Cartographer.Core/Configuration/PropertyMap.cs`)
  - Describes how a destination property is populated: convention source property, `MapFrom` expression, or ignored.

- `MapperConfiguration` (`src/Cartographer.Core/Configuration/MapperConfiguration.cs`)
  - Accepts configuration actions/profiles, builds `TypeMap` instances, applies naming conventions, strategies, attributes, validation, global options (MaxDepth, PreserveReferences, NullCollectionStrategy), and compiles delegates.
- `TypeMapExpression` / `MemberConfigurationExpression`
  - Internal fluent builders backing `CreateMap`, `ForMember`, `MapFrom`, `Ignore`, and `ReverseMap`.
- `MapCompiler` (`src/Cartographer.Core/Runtime/MapCompiler.cs`)
  - Generates expression-tree-based delegates per `TypeMap`, supporting nested mapping and collections.

## Runtime Execution
- `SimpleMapper` (`src/Cartographer.Core/Runtime/SimpleMapper.cs`)
  - Executes compiled delegates; throws if a map is missing.

## Behaviors Implemented
- Convention mapping enhanced with naming conventions, attribute-based config, and custom member matching strategies.
- Custom member options: `MapFrom`, `Ignore`, `Condition`, `PreCondition`, `ConvertUsing` (value/type converters), hooks (`BeforeMap`/`AfterMap`).
- Reverse mapping, map-into-existing-instance, recursive nested mapping, collection handling (with null strategy), inheritance (`Include`/`IncludeBase`).
- Global options: configuration validation, MaxDepth, PreserveReferences, NullCollectionStrategy.
- Performance: expression compilation per map; runtime context handles depth/reference tracking.
- DI integration: `AddCartographer()` registers configuration/mapper; profile scanning supported; sample DS usage in console and APIs.
- Tests: cover all features including validation, converters, hooks, inheritance, global options, null strategies.

## Sample Usage
- Console sample (`src/Cartographer.App`) shows all major features (hooks, conditions, naming conventions, collection strategy, global options).
- Example APIs (`example/...`) demonstrate DI integration, profile usage, inheritance, converters, and in-memory services for both .NET 9 and .NET 8.

## Potential Future Enhancements
- Open generic maps, projection support (`ProjectTo`), mapping-plan inspection, advanced DI integrations, packaging polish (NuGet metadata/README), and additional diagnostics tooling.
