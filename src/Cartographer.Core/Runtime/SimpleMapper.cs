using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;
using Cartographer.Core.Diagnostics;

namespace Cartographer.Core.Runtime;

public class SimpleMapper : IMapper
{
    private readonly Dictionary<(Type, Type), TypeMap> _maps;
    private readonly MappingOptions _options;

    public SimpleMapper(Dictionary<(Type, Type), TypeMap> maps, MappingOptions options)
    {
        _maps = maps;
        _options = options;
        Diagnostics = new MappingDiagnostics();
    }

    public MappingDiagnostics Diagnostics { get; }

    public TDestination Map<TDestination>(object source)
    {
        if (source == null) return default!;
        return (TDestination)Map(source, source.GetType(), typeof(TDestination));
    }

    public TDestination Map<TDestination>(object source, TDestination destination)
    {
        if (source == null) return destination!;
        var sourceType = source.GetType();
        var destType = destination?.GetType() ?? typeof(TDestination);

        if (!_maps.TryGetValue((sourceType, destType), out var map))
        {
            throw new InvalidOperationException($"No mapping exists from {sourceType} to {destType}");
        }

        if (map.UpdateAction == null)
        {
            throw new InvalidOperationException($"Map for {sourceType} -> {destType} does not have an update action.");
        }

        var context = new MappingContext(_options);

        var start = Diagnostics.Enabled ? Stopwatch.GetTimestamp() : 0;
        try
        {
            map.UpdateAction(source, destination!, this, context);
            if (Diagnostics.Enabled)
            {
                var elapsed = Stopwatch.GetElapsedTime(start);
                Diagnostics.Record(sourceType, destType, elapsed, true);
            }
            return destination!;
        }
        catch (Exception ex) when (Diagnostics.Enabled)
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            Diagnostics.Record(sourceType, destType, elapsed, false, ex.Message);
            throw;
        }
    }

    public object Map(object source, Type sourceType, Type destinationType)
    {
        var context = new MappingContext(_options);

        var start = Diagnostics.Enabled ? Stopwatch.GetTimestamp() : 0;
        try
        {
            var result = MapInternal(source, sourceType, destinationType, context);
            if (Diagnostics.Enabled)
            {
                var elapsed = Stopwatch.GetElapsedTime(start);
                Diagnostics.Record(sourceType, destinationType, elapsed, true);
            }
            return result;
        }
        catch (Exception ex) when (Diagnostics.Enabled)
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            Diagnostics.Record(sourceType, destinationType, elapsed, false, ex.Message);
            throw;
        }
    }

    internal object MapInternal(object source, Type sourceType, Type destinationType, MappingContext context)
    {
        var runtimeSourceType = source.GetType();
        var resolved = ResolveTypeMap(runtimeSourceType, destinationType);
        if (resolved == null)
        {
            throw new InvalidOperationException($"No mapping exists from {runtimeSourceType} to {destinationType}");
        }

        var (map, resolvedDest) = resolved.Value;
        destinationType = resolvedDest;

        if (context.Options.MaxDepth.HasValue && context.Depth >= context.Options.MaxDepth.Value)
        {
            return null!;
        }

        using var _ = context.Push();

        if (map.MappingFunc == null)
        {
            throw new InvalidOperationException($"Map for {sourceType} -> {destinationType} was not compiled.");
        }

        return map.MappingFunc(source, this, context);
    }

    public IReadOnlyList<TypeMapDescriptor> GetMappingPlans()
    {
        return _maps.Values.Select(map => new TypeMapDescriptor(
            map.SourceType,
            map.DestinationType,
            map.PropertyMaps.Select(pm => new PropertyMapDescriptor(
                pm.DestinationProperty,
                pm.SourceProperty,
                pm.SourceExpression?.ToString(),
                pm.Ignore
            )).ToList()
        )).ToList();
    }

    private (TypeMap Map, Type DestinationType)? ResolveTypeMap(Type runtimeSourceType, Type destinationType)
    {
        if (_maps.TryGetValue((runtimeSourceType, destinationType), out var exact))
        {
            return (exact, destinationType);
        }

        foreach (var kvp in _maps)
        {
            var key = kvp.Key;
            var map = kvp.Value;
            if (key.Item1.IsAssignableFrom(runtimeSourceType) && key.Item2.IsAssignableFrom(destinationType))
            {
                var derived = map.DerivedTypes.FirstOrDefault(d => d.Source == runtimeSourceType);
                if (derived != default)
                {
                    if (_maps.TryGetValue((derived.Source, derived.Destination), out var derivedMap))
                    {
                        return (derivedMap, derived.Destination);
                    }
                }
            }
        }

        return null;
    }
}
