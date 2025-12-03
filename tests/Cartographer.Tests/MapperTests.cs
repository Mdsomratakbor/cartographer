using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;
using Cartographer.Core.Configuration.Naming;
using Cartographer.Core.Configuration.Converters;
using Cartographer.Core.Configuration.Attributes;
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

    [Fact]
    public void BeforeMap_and_AfterMap_are_invoked()
    {
        var src = new DemoSource { Id = 7, Name = "BeforeAfter" };

        var dest = _mapper.Map<DemoDestination>(src);

        dest.BeforeCalled.Should().BeTrue();
        dest.AfterCalled.Should().BeTrue();
    }

    [Fact]
    public void PreCondition_skips_member_when_false()
    {
        var src = new DemoSource { Id = 8, Name = "Skip", Flag = false };

        var dest = _mapper.Map<DemoDestination>(src);

        dest.Conditional.Should().BeNull();
    }

    [Fact]
    public void Condition_skips_member_when_false()
    {
        var src = new DemoSource { Id = 9, Name = "Skip2", Flag = false };

        var dest = _mapper.Map<DemoDestination>(src);

        dest.Conditional2.Should().BeNull();
    }

    [Fact]
    public void Map_into_existing_instance_updates_values()
    {
        var src = new DemoSource { Id = 10, Name = "Existing", Flag = true };
        var dest = new DemoDestination { Id = -1, Name = "Old", Label = "OldLabel" };

        var result = _mapper.Map(src, dest);

        result.Should().BeSameAs(dest);
        dest.Id.Should().Be(10);
        dest.Name.Should().Be("Existing");
        dest.Label.Should().Be("Label: Existing");
    }

    [Fact]
    public void Naming_convention_maps_snake_to_pascal()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.SourceNamingConvention = new SnakeCaseNamingConvention();
            cfg.DestinationNamingConvention = new PascalCaseNamingConvention();
            cfg.CreateMap<SnakeSource, PascalDestination>();
        });

        var mapper = config.CreateMapper();
        var dest = mapper.Map<PascalDestination>(new SnakeSource { first_name = "Casey", last_name = "Jones" });

        dest.FirstName.Should().Be("Casey");
        dest.LastName.Should().Be("Jones");
    }

    [Fact]
    public void Member_matching_strategy_can_override_names()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.MemberMatchingStrategies.Add((src, dest) => src.Name == "CustomerName" && dest.Name == "Name");
            cfg.CreateMap<SpecialSource, SpecialDestination>();
        });

        var mapper = config.CreateMapper();
        var dest = mapper.Map<SpecialDestination>(new SpecialSource { CustomerName = "Custom" });

        dest.Name.Should().Be("Custom");
    }

    [Fact]
    public void Value_converter_maps_member()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SourceWithTextNumber, DestinationWithInt>()
                .ForMember(d => d.Number, o => o.ConvertUsing(new StringToIntConverter(), s => s.NumberText));
        });

        var mapper = config.CreateMapper();
        var dest = mapper.Map<DestinationWithInt>(new SourceWithTextNumber { NumberText = "42" });

        dest.Number.Should().Be(42);
    }

    [Fact]
    public void Type_converter_overrides_member_mapping()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ConvertibleSource, ConvertibleDestination>()
                .ConvertUsing(new CustomTypeConverter());
        });

        var mapper = config.CreateMapper();
        var dest = mapper.Map<ConvertibleDestination>(new ConvertibleSource { Value = "abc" });

        dest.Value.Should().Be("ABC");
        dest.Length.Should().Be(3);
    }

    [Fact]
    public void Attribute_based_MapFrom_and_Ignore_are_applied()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AttrSource, AttrDestination>();
        });

        var mapper = config.CreateMapper();
        var dest = mapper.Map<AttrDestination>(new AttrSource { Original = "Hello", Unused = "Bye" });

        dest.Renamed.Should().Be("Hello");
        dest.Unused.Should().BeNull();
    }

    [Fact]
    public void MaxDepth_stops_nested_mapping()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.MaxDepth = 1;
            cfg.CreateMap<Node, NodeDto>();
        });

        var mapper = config.CreateMapper();
        var root = new Node { Name = "Root", Child = new Node { Name = "Child" } };

        var dest = mapper.Map<NodeDto>(root);

        dest.Name.Should().Be("Root");
        dest.Child.Should().BeNull();
    }

    [Fact]
    public void PreserveReferences_reuses_instances()
    {
        var shared = new RefItem { Value = "Shared" };
        var config = new MapperConfiguration(cfg =>
        {
            cfg.PreserveReferences = true;
            cfg.CreateMap<RefContainer, RefContainerDto>();
            cfg.CreateMap<RefItem, RefItemDto>();
        });

        var mapper = config.CreateMapper();
        var source = new RefContainer { A = shared, B = shared };

        var dest = mapper.Map<RefContainerDto>(source);

        dest.A.Should().BeSameAs(dest.B);
    }

    [Fact]
    public void Null_collections_preserved_by_default()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CollectionSource, CollectionDestination>();
        });

        var mapper = config.CreateMapper();
        var dest = mapper.Map<CollectionDestination>(new CollectionSource { Items = null });

        dest.Items.Should().BeNull();
    }

    [Fact]
    public void Null_collections_can_be_substituted_with_empty()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.NullCollectionStrategy = NullCollectionStrategy.UseEmptyCollection;
            cfg.CreateMap<CollectionSource, CollectionDestination>();
        });

        var mapper = config.CreateMapper();
        var dest = mapper.Map<CollectionDestination>(new CollectionSource { Items = null });

        dest.Items.Should().NotBeNull();
        dest.Items!.Should().BeEmpty();
    }

    [Fact]
    public void Include_maps_derived_at_runtime()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BaseType, BaseDto>()
                .Include<ChildType, ChildDto>();
            cfg.CreateMap<ChildType, ChildDto>();
        });

        var mapper = config.CreateMapper();
        BaseType src = new ChildType { BaseValue = "base", ChildValue = "child" };

        var dest = mapper.Map<BaseDto>(src);

        dest.Should().BeOfType<ChildDto>();
        dest.BaseValue.Should().Be("base");
        ((ChildDto)dest).ChildValue.Should().Be("child");
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
            .ForMember(d => d.Conditional, o =>
            {
                o.PreCondition(s => s.Flag);
                o.MapFrom(s => s.Name);
            })
            .ForMember(d => d.Conditional2, o =>
            {
                o.Condition(s => s.Flag);
                o.MapFrom(s => s.Name);
            })
            .BeforeMap((s, d) => d.BeforeCalled = true)
            .AfterMap((s, d) => d.AfterCalled = true)
            .ReverseMap();

        cfg.CreateMap<DemoChild, DemoChildDto>()
            .ReverseMap();
    }
}

file class DemoSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Flag { get; set; }
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
    public string? Conditional { get; set; }
    public string? Conditional2 { get; set; }
    public bool BeforeCalled { get; set; }
    public bool AfterCalled { get; set; }
}

file class SnakeSource
{
    public string first_name { get; set; } = string.Empty;
    public string last_name { get; set; } = string.Empty;
}

file class PascalDestination
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

file class SpecialSource
{
    public string CustomerName { get; set; } = string.Empty;
}

file class SpecialDestination
{
    public string Name { get; set; } = string.Empty;
}

file class SourceWithTextNumber
{
    public string NumberText { get; set; } = string.Empty;
}

file class DestinationWithInt
{
    public int Number { get; set; }
}

file class StringToIntConverter : IValueConverter<string, int>
{
    public int Convert(string sourceMember) => int.TryParse(sourceMember, out var n) ? n : 0;
}

file class ConvertibleSource
{
    public string Value { get; set; } = string.Empty;
}

file class ConvertibleDestination
{
    public string Value { get; set; } = string.Empty;
    public int Length { get; set; }
}

file class CustomTypeConverter : ITypeConverter<ConvertibleSource, ConvertibleDestination>
{
    public ConvertibleDestination Convert(ConvertibleSource source) =>
        new ConvertibleDestination
        {
            Value = source.Value.ToUpperInvariant(),
            Length = source.Value.Length
        };
}

file class AttrSource
{
    public string Original { get; set; } = string.Empty;
    public string Unused { get; set; } = string.Empty;
}

file class AttrDestination
{
    [MapFrom("Original")]
    public string Renamed { get; set; } = string.Empty;

    [IgnoreMap]
    public string? Unused { get; set; }
}

file class Node
{
    public string Name { get; set; } = string.Empty;
    public Node? Child { get; set; }
}

file class NodeDto
{
    public string Name { get; set; } = string.Empty;
    public NodeDto? Child { get; set; }
}

file class RefItem
{
    public string Value { get; set; } = string.Empty;
}

file class RefItemDto
{
    public string Value { get; set; } = string.Empty;
}

file class RefContainer
{
    public RefItem? A { get; set; }
    public RefItem? B { get; set; }
}

file class RefContainerDto
{
    public RefItemDto? A { get; set; }
    public RefItemDto? B { get; set; }
}

file class CollectionSource
{
    public IEnumerable<string>? Items { get; set; }
}

file class CollectionDestination
{
    public List<string>? Items { get; set; }
}

file class BaseType
{
    public string BaseValue { get; set; } = string.Empty;
}

file class ChildType : BaseType
{
    public string ChildValue { get; set; } = string.Empty;
}

file class BaseDto
{
    public string BaseValue { get; set; } = string.Empty;
}

file class ChildDto : BaseDto
{
    public string ChildValue { get; set; } = string.Empty;
}

file class DemoChild
{
    public string Value { get; set; } = string.Empty;
}

file class DemoChildDto
{
    public string Value { get; set; } = string.Empty;
}
