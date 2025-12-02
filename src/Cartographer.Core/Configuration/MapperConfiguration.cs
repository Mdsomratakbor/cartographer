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

    /// <summary>
    /// Validates all configured maps and throws <see cref="ConfigurationValidationException"/> if any issues are found.
    /// </summary>
    public void AssertConfigurationIsValid()
    {
        var errors = new List<string>();

        foreach (var map in _maps.Values)
        {
            foreach (var propertyMap in map.PropertyMaps)
            {
                if (propertyMap.Ignore)
                {
                    continue;
                }

                var destinationType = propertyMap.DestinationProperty.PropertyType;
                var sourceType = propertyMap.SourceExpression?.ReturnType ?? propertyMap.SourceProperty?.PropertyType;

                if (sourceType == null)
                {
                    errors.Add($"No source for destination member {map.DestinationType.Name}.{propertyMap.DestinationProperty.Name}");
                    continue;
                }

                if (destinationType.IsAssignableFrom(sourceType))
                {
                    continue;
                }

                if (HasDirectMap(sourceType, destinationType))
                {
                    continue;
                }

                if (TryValidateEnumerableMapping(sourceType, destinationType, out var collectionError))
                {
                    continue;
                }

                errors.Add(collectionError ?? $"Cannot map {sourceType.Name} to {destinationType.Name} for member {map.DestinationType.Name}.{propertyMap.DestinationProperty.Name}");
            }
        }

        if (errors.Count > 0)
        {
            throw new ConfigurationValidationException(errors);
        }
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

    private bool HasDirectMap(Type source, Type dest) => _maps.ContainsKey((source, dest));

    private bool TryValidateEnumerableMapping(Type sourceType, Type destinationType, out string? error)
    {
        error = null;
        var srcElement = GetEnumerableElementType(sourceType);
        var destElement = GetEnumerableElementType(destinationType);

        if (srcElement == null || destElement == null)
        {
            return false;
        }

        if (destElement.IsAssignableFrom(srcElement))
        {
            return true;
        }

        if (HasDirectMap(srcElement, destElement))
        {
            return true;
        }

        error = $"Cannot map collection element {srcElement.Name} to {destElement.Name} for member type {destinationType.Name}";
        return false;
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return type.GetGenericArguments()[0];
        }

        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return iface.GetGenericArguments()[0];
            }
        }

        return null;
    }
}
