using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;
using Cartographer.Core.Configuration.Converters;
using Cartographer.Core.Configuration.Naming;

BenchmarkRunner.Run<MappingBenchmarks>();

[MemoryDiagnoser]
public class MappingBenchmarks
{
    private IMapper _mapper = default!;

    private SimpleSource _simple = default!;
    private NestedSource _nested = default!;
    private ConverterSource _converterSrc = default!;
    private DerivedSource _derived = default!;
    private SimpleDestination _existing = default!;

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
                .Include<DerivedSource, DerivedDestination>();
            cfg.CreateMap<DerivedSource, DerivedDestination>();
        });

        _mapper = config.CreateMapper();

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
    public BaseDestination Map_Inheritance() => _mapper.Map<BaseDestination>(_derived);
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
    public string BaseValue { get; set; } = string.Empty;
}

public class DerivedDestination : BaseDestination
{
    public string DerivedValue { get; set; } = string.Empty;
}

public class StringToIntConverter : IValueConverter<string, int>
{
    public int Convert(string sourceMember) => int.TryParse(sourceMember, out var n) ? n : 0;
}
