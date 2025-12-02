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

    public TDestination Map<TDestination>(object source)
    {
        if (source == null) return default!;
        return (TDestination)Map(source, source.GetType(), typeof(TDestination));
    }

    public object Map(object source, Type sourceType, Type destinationType)
    {
        if (!_maps.TryGetValue((sourceType, destinationType), out var map))
        {
            throw new InvalidOperationException($"No mapping exists from {sourceType} to {destinationType}");
        }

        var dest = Activator.CreateInstance(destinationType) ?? throw new InvalidOperationException($"Cannot create {destinationType}");

        foreach (var propertyMap in map.PropertyMaps)
        {
            if (propertyMap.Ignore)
            {
                continue;
            }

            object? value = null;

            if (propertyMap.SourceExpression != null)
            {
                var compiled = propertyMap.SourceExpression.Compile();
                value = compiled.DynamicInvoke(source);
            }
            else if (propertyMap.SourceProperty != null)
            {
                value = propertyMap.SourceProperty.GetValue(source);
            }
            else
            {
                continue;
            }

            propertyMap.DestinationProperty.SetValue(dest, value);
        }

        return dest;
    }
}
