using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;
using Cartographer.Core.Configuration.Converters;
using Cartographer.Core.Configuration.Naming;
using Cartographer.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

BenchmarkRunner.Run<MappingBenchmarks>();

[MemoryDiagnoser]
public class MappingBenchmarks
{
    private IMapper _mapper = default!;
    private IMapper _referenceMapper = default!;

    private SimpleSource _simple = default!;
    private NestedSource _nested = default!;
    private ConverterSource _converterSrc = default!;
    private DerivedSource _derived = default!;
    private SimpleDestination _existing = default!;
    private ReverseNamedDestination _reverseDestination = default!;
    private DerivedSource _includeBaseSource = default!;
    private ReferenceNode _referenceRoot = default!;

    [GlobalSetup]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.SourceNamingConvention = new SnakeCaseNamingConvention();
            cfg.DestinationNamingConvention = new PascalCaseNamingConvention();
            cfg.NullCollectionStrategy = NullCollectionStrategy.UseEmptyCollection;

            cfg.CreateMap<SimpleSource, SimpleDestination>()
                .ForMember(d => d.DisplayName, o => o.MapFrom(s => $"{s.First} {s.Last}"));

            cfg.CreateMap<NestedSource, NestedDestination>();

            cfg.CreateMap<ConverterSource, ConverterDestination>()
                .ForMember(d => d.Number, o => o.ConvertUsing(new StringToIntConverter(), s => s.Number));

            cfg.CreateMap<BaseSource, BaseDestination>()
                .ForMember(d => d.Renamed, o => o.MapFrom(s => s.BaseValue))
                .Include<DerivedSource, DerivedDestination>();

            cfg.CreateMap<DerivedSource, DerivedDestination>()
                .IncludeBase<BaseSource, BaseDestination>();

            cfg.CreateMap<ReverseNamedSource, ReverseNamedDestination>()
                .ForMember(d => d.DisplayName, o => o.MapFrom(s => s.Name))
                .ReverseMap();
        });

        _mapper = config.CreateMapper();

        var referenceConfig = new MapperConfiguration(cfg =>
        {
            cfg.PreserveReferences = true;
            cfg.CreateMap<ReferenceNode, ReferenceNodeDto>();
        });

        _referenceMapper = referenceConfig.CreateMapper();

        _simple = new SimpleSource { First = "Ada", Last = "Lovelace", Age = 36 };
        _existing = new SimpleDestination();

        _nested = new NestedSource
        {
            Title = "Parent",
            Child = new SimpleSource { First = "Child", Last = "One", Age = 10 },
            Children = new[]
            {
                new SimpleSource { First = "C1", Last = "Last", Age = 8 },
                new SimpleSource { First = "C2", Last = "Last", Age = 6 }
            }
        };

        _converterSrc = new ConverterSource { Number = "123" };
        _derived = new DerivedSource { BaseValue = "base", DerivedValue = "derived" };
        _includeBaseSource = new DerivedSource { BaseValue = "inherited", DerivedValue = "payload" };
        _reverseDestination = new ReverseNamedDestination { DisplayName = "Reverse" };

        _referenceRoot = new ReferenceNode { Name = "root" };
        var child = new ReferenceNode { Name = "child", Parent = _referenceRoot };
        _referenceRoot.Child = child;
    }

    [Benchmark]
    public SimpleDestination Map_Simple() => _mapper.Map<SimpleDestination>(_simple);

    [Benchmark]
    public SimpleDestination Map_Simple_Existing()
    {
        _mapper.Map(_simple, _existing);
        return _existing;
    }

    [Benchmark]
    public NestedDestination Map_Nested() => _mapper.Map<NestedDestination>(_nested);

    [Benchmark]
    public ConverterDestination Map_Converter() => _mapper.Map<ConverterDestination>(_converterSrc);

    [Benchmark]
    public BaseDestination Map_Inheritance()
    {
        BaseSource source = _derived;
        return _mapper.Map<BaseDestination>(source);
    }

    [Benchmark]
    public DerivedDestination Map_IncludeBase() => _mapper.Map<DerivedDestination>(_includeBaseSource);

    [Benchmark]
    public ReverseNamedSource Map_ReverseMap() => _mapper.Map<ReverseNamedSource>(_reverseDestination);

    [Benchmark]
    public ReferenceNodeDto Map_PreserveReferences_Cycle() => _referenceMapper.Map<ReferenceNodeDto>(_referenceRoot);

    [Benchmark]
    public IMapper Build_Mapper_With_ProfileScanning()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBenchmarkFormatter, BenchmarkFormatter>();
        services.AddCartographer(cfg =>
        {
            cfg.SourceNamingConvention = new SnakeCaseNamingConvention();
            cfg.DestinationNamingConvention = new PascalCaseNamingConvention();
        }, typeof(BenchmarkProfile).Assembly);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMapper>();
    }
}

public class SimpleSource
{
    public string First { get; set; } = string.Empty;
    public string Last { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class SimpleDestination
{
    public string DisplayName { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class NestedSource
{
    public string Title { get; set; } = string.Empty;
    public SimpleSource? Child { get; set; }
    public IEnumerable<SimpleSource>? Children { get; set; }
}

public class NestedDestination
{
    public string Title { get; set; } = string.Empty;
    public SimpleDestination? Child { get; set; }
    public List<SimpleDestination> Children { get; set; } = new();
}

public class ConverterSource
{
    public string Number { get; set; } = string.Empty;
}

public class ConverterDestination
{
    public int Number { get; set; }
}

public class BaseSource
{
    public string BaseValue { get; set; } = string.Empty;
}

public class DerivedSource : BaseSource
{
    public string DerivedValue { get; set; } = string.Empty;
}

public class BaseDestination
{
    public string Renamed { get; set; } = string.Empty;
}

public class DerivedDestination : BaseDestination
{
    public string DerivedValue { get; set; } = string.Empty;
}

public class ReverseNamedSource
{
    public string Name { get; set; } = string.Empty;
}

public class ReverseNamedDestination
{
    public string DisplayName { get; set; } = string.Empty;
}

public class ReferenceNode
{
    public string Name { get; set; } = string.Empty;
    public ReferenceNode? Parent { get; set; }
    public ReferenceNode? Child { get; set; }
}

public class ReferenceNodeDto
{
    public string Name { get; set; } = string.Empty;
    public ReferenceNodeDto? Parent { get; set; }
    public ReferenceNodeDto? Child { get; set; }
}

public class ProfileSource
{
    public string first_name { get; set; } = string.Empty;
    public string last_name { get; set; } = string.Empty;
}

public class ProfileDestination
{
    public string DisplayName { get; set; } = string.Empty;
}

public class StringToIntConverter : IValueConverter<string, int>
{
    public int Convert(string sourceMember) => int.TryParse(sourceMember, out var n) ? n : 0;
}

public interface IBenchmarkFormatter
{
    string Format(string firstName, string lastName);
}

public sealed class BenchmarkFormatter : IBenchmarkFormatter
{
    public string Format(string firstName, string lastName) => $"{firstName} {lastName}";
}

public sealed class BenchmarkProfile : Profile
{
    private readonly IBenchmarkFormatter _formatter;

    public BenchmarkProfile(IBenchmarkFormatter formatter)
    {
        _formatter = formatter;
    }

    protected override void ConfigureMappings(IMapperConfigurationExpression cfg)
    {
        cfg.CreateMap<ProfileSource, ProfileDestination>()
            .ForMember(d => d.DisplayName, o => o.MapFrom(s => _formatter.Format(s.first_name, s.last_name)));
    }
}
