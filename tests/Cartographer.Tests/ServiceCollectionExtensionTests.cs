using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;
using Cartographer.Core.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Cartographer.Tests;

public class ServiceCollectionExtensionTests
{
    [Fact]
    public void AddCartographer_profile_scanning_supports_constructor_injection()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new InjectedLabelFormatter("DI"));
        services.AddCartographer(typeof(ConstructorInjectedProfile).Assembly);

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var dest = mapper.Map<InjectedDestination>(new InjectedSource { Name = "Ada" });

        dest.Label.Should().Be("DI:Ada");
    }
}

file class InjectedSource
{
    public string Name { get; set; } = string.Empty;
}

file class InjectedDestination
{
    public string Label { get; set; } = string.Empty;
}

public sealed class InjectedLabelFormatter
{
    private readonly string _prefix;

    public InjectedLabelFormatter(string prefix)
    {
        _prefix = prefix;
    }

    public string Format(string value) => $"{_prefix}:{value}";
}

public sealed class ConstructorInjectedProfile : Profile
{
    private readonly InjectedLabelFormatter _formatter;

    public ConstructorInjectedProfile(InjectedLabelFormatter formatter)
    {
        _formatter = formatter;
    }

    protected override void ConfigureMappings(IMapperConfigurationExpression cfg)
    {
        cfg.CreateMap<InjectedSource, InjectedDestination>()
            .ForMember(d => d.Label, o => o.MapFrom(s => _formatter.Format(s.Name)));
    }
}
