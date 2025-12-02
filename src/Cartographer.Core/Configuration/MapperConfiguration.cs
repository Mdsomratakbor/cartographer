using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cartographer.Core.Abstractions;
using Cartographer.Core.Runtime;

namespace Cartographer.Core.Configuration;

public class MapperConfiguration : IMapperConfigurationExpression
{
    private readonly Dictionary<(Type, Type), TypeMap> _maps = new();

    /// <summary>
    /// Creates a mapper configuration using the provided configuration action.
    /// </summary>
    /// <param name="config">Action that defines all maps.</param>
    public MapperConfiguration(Action<IMapperConfigurationExpression> config)
    {
        config(this);
        BuildConventionMaps();
    }

    /// <summary>
    /// Creates or retrieves a type map between source and destination.
    /// </summary>
    public ITypeMapExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
    {
        var map = GetOrCreate(typeof(TSource), typeof(TDestination));
        return new TypeMapExpression<TSource, TDestination>(map, this);
    }

    /// <summary>
    /// Builds compiled delegates for all maps and produces an <see cref="IMapper"/>.
    /// </summary>
    public IMapper CreateMapper()
    {
        var compiler = new MapCompiler(_maps);
        compiler.CompileAll();
        return new SimpleMapper(_maps);
    }

    internal TypeMap GetMap(Type source, Type dest) => _maps[(source, dest)];

    private TypeMap GetOrCreate(Type src, Type dest)
    {
        if (_maps.TryGetValue((src, dest), out var existing))
        {
            return existing;
        }

        var map = new TypeMap(src, dest);
        _maps[(src, dest)] = map;
        return map;
    }

    private void BuildConventionMaps()
    {
        foreach (var map in _maps.Values)
        {
            var destProps = map.DestinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);
            var srcProps = map.SourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);

            foreach (var destProp in destProps)
            {
                var propertyMap = map.PropertyMaps.FirstOrDefault(p => p.DestinationProperty == destProp);
                if (propertyMap == null)
                {
                    propertyMap = new PropertyMap(destProp);
                    map.PropertyMaps.Add(propertyMap);
                }

                if (propertyMap.SourceProperty != null)
                {
                    continue;
                }

                if (srcProps.TryGetValue(destProp.Name, out var sourceProp))
                {
                    propertyMap.SourceProperty = sourceProp;
                }
            }
        }
    }
}
