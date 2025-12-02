using Cartographer.Core.Configuration;
using FluentAssertions;

namespace Cartographer.Tests;

public class ConfigurationValidationTests
{
    [Fact]
    public void AssertConfigurationIsValid_passes_for_valid_maps()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ValidSource, ValidDestination>();
        });

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertConfigurationIsValid_fails_when_destination_member_unmapped()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ValidSource, DestinationWithUnmapped>();
        });

        var act = () => config.AssertConfigurationIsValid();

        act.Should().Throw<ConfigurationValidationException>()
            .Which.Errors.Should().Contain(e => e.Contains("DestinationWithUnmapped.Unmapped"));
    }

    [Fact]
    public void AssertConfigurationIsValid_fails_when_type_incompatible_without_nested_map()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SourceWithMismatchedType, DestinationWithMismatch>();
        });

        var act = () => config.AssertConfigurationIsValid();

        act.Should().Throw<ConfigurationValidationException>()
            .Which.Errors.Should().Contain(e => e.Contains("Cannot map"));
    }

    [Fact]
    public void AssertConfigurationIsValid_passes_for_nested_map_when_map_exists()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ValidSourceWithChild, DestinationWithChildDto>();
            cfg.CreateMap<Child, ChildDto>();
        });

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }
}

file class ValidSource
{
    public string Name { get; set; } = string.Empty;
}

file class ValidDestination
{
    public string Name { get; set; } = string.Empty;
}

file class SourceWithMismatchedType
{
    public string Name { get; set; } = string.Empty;
    public string Count { get; set; } = string.Empty;
}

file class DestinationWithUnmapped
{
    public string Name { get; set; } = string.Empty;
    public string Unmapped { get; set; } = string.Empty;
}

file class DestinationWithMismatch
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

file class ValidSourceWithChild
{
    public Child Child { get; set; } = new();
}

file class DestinationWithChildDto
{
    public ChildDto Child { get; set; } = new();
}

file class Child
{
    public string Value { get; set; } = string.Empty;
}

file class ChildDto
{
    public string Value { get; set; } = string.Empty;
}
