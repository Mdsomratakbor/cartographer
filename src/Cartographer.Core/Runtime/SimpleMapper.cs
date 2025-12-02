using System;
using System.Collections.Generic;
using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;

namespace Cartographer.Core.Runtime;

public class SimpleMapper : IMapper
{
    private readonly Dictionary<(Type, Type), TypeMap> _maps;

    public SimpleMapper(Dictionary<(Type, Type), TypeMap> maps)
    {
        _maps = maps;
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
    /// Maps the given source object to a new destination instance of <paramref name="destinationType"/>.
    /// </summary>
    public object Map(object source, Type sourceType, Type destinationType)
    {
        if (!_maps.TryGetValue((sourceType, destinationType), out var map))
        {
            throw new InvalidOperationException($"No mapping exists from {sourceType} to {destinationType}");
        }

        if (map.MappingFunc == null)
        {
            throw new InvalidOperationException($"Map for {sourceType} -> {destinationType} was not compiled.");
        }

        return map.MappingFunc(source, this);
    }
}
