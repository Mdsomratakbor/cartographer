# Cartographer.Mapper

AutoMapper-style object mapper for .NET 8/9 with profiles, fluent configuration, converters, inheritance support, global options, and DI integration.

## Install
```bash
dotnet add package Cartographer.Mapper
```

## Quick start
```csharp
using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;

public class UserProfile : Profile
{
    protected override void ConfigureMappings(IMapperConfigurationExpression cfg)
    {
        cfg.CreateMap<User, UserDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.Nickname, o => { o.Condition(s => !string.IsNullOrEmpty(s.Nickname)); o.MapFrom(s => s.Nickname); })
            .BeforeMap((s, d) => d.Trace = "before")
            .AfterMap((s, d) => d.Trace = "after")
            .ReverseMap();
    }
}

var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<User, UserDto>();
    new UserProfile().Apply(cfg);
});

var mapper = config.CreateMapper();
var dto = mapper.Map<UserDto>(new User { FirstName = "Ada", LastName = "Lovelace" });
```

## ASP.NET Core integration
```csharp
using Cartographer.Core.DependencyInjection;
using Cartographer.Core.Configuration.Naming;
using Cartographer.Core.Configuration;

builder.Services.AddCartographer(cfg =>
{
    cfg.SourceNamingConvention = new SnakeCaseNamingConvention();
    cfg.DestinationNamingConvention = new PascalCaseNamingConvention();
    cfg.MaxDepth = 3;
    cfg.PreserveReferences = true;
    cfg.NullCollectionStrategy = NullCollectionStrategy.UseEmptyCollection;
    new UserProfile().Apply(cfg);
});
```

## Features
- Familiar API: `CreateMap`, `ForMember` (`MapFrom`, `Ignore`, `Condition`, `PreCondition`, `ConvertUsing`), `ReverseMap`.
- Hooks: `BeforeMap`, `AfterMap`.
- Inheritance: `Include`, `IncludeBase`.
- Converters: value converters and type converters.
- Global options: `MaxDepth`, `PreserveReferences`, `NullCollectionStrategy`.
- Naming conventions and custom member matching strategies.
- Attribute-based config: `[MapFrom]`, `[IgnoreMap]`.
- Map into existing instances.
- Expression-compiled delegates for performance.
- DI extensions with profile scanning.
- Multi-target: net8.0 and net9.0.

## Examples
- Console sample: `src/Cartographer.App`.
- Web APIs: `example/Cartographer.Example.Api` (net9) and `example/Cartographer.Example.Net8Api` (net8) with in-memory services/controllers showing DI + profiles + inheritance.

## Package info
- Package ID: `Cartographer.Mapper`
- License: MIT
- Repository: [CartoGrapher](https://github.com/Mdsomratakbor/cartographer)
