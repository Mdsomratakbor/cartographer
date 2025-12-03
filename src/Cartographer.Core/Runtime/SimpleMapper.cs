using System;
using System.Collections.Generic;
using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;

namespace Cartographer.Core.Runtime;

public class SimpleMapper : IMapper
{
    private readonly Dictionary<(Type, Type), TypeMap> _maps;
    private readonly MappingOptions _options;

    public SimpleMapper(Dictionary<(Type, Type), TypeMap> maps, MappingOptions options)
    {
        _maps = maps;
        _options = options;
    }

    /// <summary>
    /// Maps the given source object to a new destination instance of <typeparamref name="TDestination"/>.
    /// </summary>
    public TDestination Map<TDestination>(object source)
    {
        if (source == null) return default!;
        return (TDestination)Map(source, source.GetType(), typeof(TDestination));
    }

    /// <summary>
    /// Maps the given source object into an existing destination instance.
    /// </summary>
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
        map.UpdateAction(source, destination!, this, context);
        return destination!;
    }

    /// <summary>
    /// Maps the given source object to a new destination instance of <paramref name="destinationType"/>.
    /// </summary>
    public object Map(object source, Type sourceType, Type destinationType)
    {
        var context = new MappingContext(_options);
        return MapInternal(source, sourceType, destinationType, context);
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

    private (TypeMap Map, Type DestinationType)? ResolveTypeMap(Type runtimeSourceType, Type destinationType)
    {
        // Exact match
        if (_maps.TryGetValue((runtimeSourceType, destinationType), out var exact))
        {
            return (exact, destinationType);
        }

        // Look for a base map that includes derived types
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
