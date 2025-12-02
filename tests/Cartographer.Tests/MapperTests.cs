using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;
using FluentAssertions;

namespace Cartographer.Tests;

public class MapperTests
{
    private readonly IMapper _mapper;

    public MapperTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            new DemoProfile().Apply(cfg);
        });

        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Maps_by_convention()
    {
        var src = new DemoSource { Id = 1, Name = "Alpha" };

        var dest = _mapper.Map<DemoDestination>(src);

        dest.Id.Should().Be(1);
        dest.Name.Should().Be("Alpha");
    }

    [Fact]
    public void Applies_MapFrom()
    {
        var src = new DemoSource { Id = 2, Name = "Ada" };

        var dest = _mapper.Map<DemoDestination>(src);

        dest.Label.Should().Be("Label: Ada");
    }

    [Fact]
    public void Honors_Ignore()
    {
        var src = new DemoSource { Id = 3, Name = "Ignored" };

        var dest = _mapper.Map<DemoDestination>(src);

        dest.Ignored.Should().BeNull();
    }

    [Fact]
    public void ReverseMap_creates_inverse_map()
    {
        var dest = new DemoDestination { Id = 4, Name = "Zoe", Label = "Label: Zoe" };

        var src = (DemoSource)_mapper.Map(dest, typeof(DemoDestination), typeof(DemoSource));

        src.Id.Should().Be(4);
        src.Name.Should().Be("Zoe");
    }

    [Fact]
    public void Maps_nested_types_recursively()
    {
        var src = new DemoSource
        {
            Id = 5,
            Name = "Parent",
            Child = new DemoChild { Value = "Nested" }
        };

        var dest = _mapper.Map<DemoDestination>(src);

        dest.Child.Should().NotBeNull();
        dest.Child!.Value.Should().Be("Nested");
    }

    [Fact]
    public void Maps_collections_recursively()
    {
        var src = new DemoSource
        {
            Id = 6,
            Name = "Parent",
            Children = new[]
            {
                new DemoChild { Value = "One" },
                new DemoChild { Value = "Two" }
            }
        };

        var dest = _mapper.Map<DemoDestination>(src);

        dest.Children.Should().NotBeNull();
        dest.Children!.Count.Should().Be(2);
        dest.Children[0].Value.Should().Be("One");
        dest.Children[1].Value.Should().Be("Two");
    }
}

file static class MapperTestExtensions
{
    public static TDest Map<TDest>(this IMapper mapper, object source)
    {
        return mapper.Map<TDest>(source);
    }
}

file class DemoProfile : Profile
{
    protected override void ConfigureMappings(IMapperConfigurationExpression cfg)
    {
        cfg.CreateMap<DemoSource, DemoDestination>()
            .ForMember(d => d.Label, o => o.MapFrom(s => $"Label: {s.Name}"))
            .ForMember(d => d.Ignored, o => o.Ignore())
            .ReverseMap();

        cfg.CreateMap<DemoChild, DemoChildDto>()
            .ReverseMap();
    }
}

file class DemoSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DemoChild? Child { get; set; }
    public IEnumerable<DemoChild>? Children { get; set; }
}

file class DemoDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Ignored { get; set; }
    public DemoChildDto? Child { get; set; }
    public List<DemoChildDto>? Children { get; set; }
}

file class DemoChild
{
    public string Value { get; set; } = string.Empty;
}

file class DemoChildDto
{
    public string Value { get; set; } = string.Empty;
}
