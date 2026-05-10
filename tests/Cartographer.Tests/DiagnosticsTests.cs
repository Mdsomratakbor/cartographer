using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;
using Cartographer.Core.Diagnostics;
using FluentAssertions;

namespace Cartographer.Tests;

public class DiagnosticsTests
{
    [Fact]
    public void GetMappingPlans_returns_all_type_maps()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<DiagSource, DiagDest>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.First} {s.Last}"));
        });

        var mapper = config.CreateMapper();
        var plans = mapper.GetMappingPlans();

        plans.Should().HaveCount(1);
        var plan = plans[0];
        plan.SourceType.Should().Be(typeof(DiagSource));
        plan.DestinationType.Should().Be(typeof(DiagDest));
    }

    [Fact]
    public void GetMappingPlans_lists_property_maps()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<DiagSource, DiagDest>()
                .ForMember(d => d.IgnoredField, o => o.Ignore());
        });

        var mapper = config.CreateMapper();
        var plans = mapper.GetMappingPlans();
        var plan = plans[0];

        plan.Properties.Should().Contain(p => p.DestinationProperty.Name == "FullName");
        plan.Properties.Should().Contain(p => p.DestinationProperty.Name == "IgnoredField" && p.IsIgnored);
    }

    [Fact]
    public void Diagnostics_records_mapping_when_enabled()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<DiagSource, DiagDest>();
        })
        { EnableDiagnostics = true };

        var mapper = config.CreateMapper();
        mapper.Diagnostics.Enabled.Should().BeTrue();
        mapper.Diagnostics.TotalMappings.Should().Be(0);

        mapper.Map<DiagDest>(new DiagSource { First = "A", Last = "B" });

        mapper.Diagnostics.TotalMappings.Should().Be(1);
        mapper.Diagnostics.FailedMappings.Should().Be(0);
        var entry = mapper.Diagnostics.Entries[0];
        entry.SourceType.Should().Be(typeof(DiagSource));
        entry.DestinationType.Should().Be(typeof(DiagDest));
        entry.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Diagnostics_does_not_record_when_disabled()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<DiagSource, DiagDest>();
        });

        var mapper = config.CreateMapper();
        mapper.Diagnostics.Enabled.Should().BeFalse();

        mapper.Map<DiagDest>(new DiagSource { First = "A", Last = "B" });

        mapper.Diagnostics.TotalMappings.Should().Be(0);
    }

    [Fact]
    public void Diagnostics_records_failed_mapping()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<DiagSource, DiagDest>();
        })
        { EnableDiagnostics = true };

        var mapper = config.CreateMapper();

        Assert.Throws<InvalidOperationException>(() =>
            mapper.Map<DiagDest>(new object()));

        mapper.Diagnostics.FailedMappings.Should().Be(1);
        mapper.Diagnostics.Entries[0].IsSuccess.Should().BeFalse();
        mapper.Diagnostics.Entries[0].ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public void Diagnostics_clear_resets_entries()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<DiagSource, DiagDest>();
        })
        { EnableDiagnostics = true };

        var mapper = config.CreateMapper();
        mapper.Map<DiagDest>(new DiagSource { First = "A", Last = "B" });
        mapper.Diagnostics.TotalMappings.Should().Be(1);

        mapper.Diagnostics.Clear();
        mapper.Diagnostics.TotalMappings.Should().Be(0);
    }

    [Fact]
    public void Diagnostics_records_map_into_existing()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<DiagSource, DiagDest>();
        })
        { EnableDiagnostics = true };

        var mapper = config.CreateMapper();
        var dest = new DiagDest();
        mapper.Map(new DiagSource { First = "X", Last = "Y" }, dest);

        mapper.Diagnostics.TotalMappings.Should().Be(1);
        mapper.Diagnostics.Entries[0].IsSuccess.Should().BeTrue();
    }

    private class DiagSource
    {
        public string First { get; set; } = string.Empty;
        public string Last { get; set; } = string.Empty;
    }

    private class DiagDest
    {
        public string FullName { get; set; } = string.Empty;
        public string? IgnoredField { get; set; }
    }
}
