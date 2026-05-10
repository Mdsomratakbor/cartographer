using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration.Naming;
using Cartographer.Core.Configuration.Attributes;
using Cartographer.Core.Runtime;

namespace Cartographer.Core.Configuration;

public class MapperConfiguration : IMapperConfigurationExpression
{
    private readonly Dictionary<(Type, Type), TypeMap> _maps = new();

    public INamingConvention SourceNamingConvention { get; set; } = new IdentityNamingConvention();
    public INamingConvention DestinationNamingConvention { get; set; } = new IdentityNamingConvention();
    public IList<Func<PropertyInfo, PropertyInfo, bool>> MemberMatchingStrategies { get; } = new List<Func<PropertyInfo, PropertyInfo, bool>>();
    public int? MaxDepth { get; set; }
    public bool PreserveReferences { get; set; }
    public NullCollectionStrategy NullCollectionStrategy { get; set; } = NullCollectionStrategy.PreserveNull;
    public bool EnableDiagnostics { get; set; }

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
        var options = new MappingOptions
        {
            MaxDepth = MaxDepth,
            PreserveReferences = PreserveReferences,
            NullCollectionStrategy = NullCollectionStrategy
        };
        var mapper = new SimpleMapper(_maps, options);
        mapper.Diagnostics.Enabled = EnableDiagnostics;
        return mapper;
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

    internal TypeMap GetOrCreate(Type src, Type dest)
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
        ApplyIncludedBaseMaps();

        foreach (var map in _maps.Values)
        {
            var destProps = map.DestinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);
            var srcProps = map.SourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);

            var normalizedSource = srcProps.Values.ToDictionary(p => SourceNamingConvention.Normalize(p.Name), p => p, StringComparer.Ordinal);

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

                if (TryApplyAttributes(propertyMap, srcProps))
                {
                    continue;
                }

                if (TryMatchByStrategy(destProp, srcProps.Values, out var strategyProp))
                {
                    propertyMap.SourceProperty = strategyProp;
                    propertyMap.SourceExpression = null;
                    continue;
                }

                var normalizedDestName = DestinationNamingConvention.Normalize(destProp.Name);
                if (normalizedSource.TryGetValue(normalizedDestName, out var sourceProp))
                {
                    propertyMap.SourceProperty = sourceProp;
                    propertyMap.SourceExpression = null;
                }
            }
        }
    }

    internal void ConfigureReverseMap(TypeMap forwardMap, TypeMap reverseMap)
    {
        ConfigureReverseMap(forwardMap, reverseMap, new HashSet<(Type Source, Type Destination)>());
    }

    private bool HasDirectMap(Type source, Type dest) => _maps.ContainsKey((source, dest));

    private void ConfigureReverseMap(TypeMap forwardMap, TypeMap reverseMap, HashSet<(Type Source, Type Destination)> visited)
    {
        if (!visited.Add((forwardMap.SourceType, forwardMap.DestinationType)))
        {
            return;
        }

        foreach (var propertyMap in forwardMap.PropertyMaps)
        {
            var reverseTarget = ResolveReverseTargetProperty(forwardMap, propertyMap);
            if (reverseTarget == null)
            {
                continue;
            }

            var reversePropertyMap = FindOrAddPropertyMap(reverseMap, reverseTarget);

            if (propertyMap.Ignore)
            {
                reversePropertyMap.Ignore = true;
                continue;
            }

            if (propertyMap.ValueConverter != null)
            {
                continue;
            }

            if (reversePropertyMap.SourceExpression != null || reversePropertyMap.SourceProperty != null)
            {
                continue;
            }

            var reverseSourceExpression = BuildReverseSourceExpression(reverseMap.SourceType, propertyMap.DestinationProperty);
            if (reverseSourceExpression == null)
            {
                continue;
            }

            reversePropertyMap.SourceExpression = reverseSourceExpression;
            reversePropertyMap.SourceProperty = null;
        }

        if (forwardMap.IncludedBaseMap != null)
        {
            var reverseBaseMap = GetOrCreate(forwardMap.IncludedBaseMap.DestinationType, forwardMap.IncludedBaseMap.SourceType);
            reverseMap.IncludedBaseMap = reverseBaseMap;

            var derived = (reverseMap.SourceType, reverseMap.DestinationType);
            if (!reverseBaseMap.DerivedTypes.Contains(derived))
            {
                reverseBaseMap.DerivedTypes.Add(derived);
            }

            ConfigureReverseMap(forwardMap.IncludedBaseMap, reverseBaseMap, visited);
        }

        foreach (var derived in forwardMap.DerivedTypes)
        {
            var reverseDerived = GetOrCreate(derived.Destination, derived.Source);
            var reverseInclude = (derived.Destination, derived.Source);
            if (!reverseMap.DerivedTypes.Contains(reverseInclude))
            {
                reverseMap.DerivedTypes.Add(reverseInclude);
            }

            if (_maps.TryGetValue((derived.Source, derived.Destination), out var derivedMap))
            {
                ConfigureReverseMap(derivedMap, reverseDerived, visited);
            }
        }
    }

    private void ApplyIncludedBaseMaps()
    {
        foreach (var map in _maps.Values)
        {
            ApplyIncludedBaseMap(map, new HashSet<TypeMap>());
        }
    }

    private void ApplyIncludedBaseMap(TypeMap map, ISet<TypeMap> visited)
    {
        if (map.IncludedBaseMap == null || !visited.Add(map))
        {
            return;
        }

        ApplyIncludedBaseMap(map.IncludedBaseMap, visited);
        MergeBaseMap(map, map.IncludedBaseMap);
    }

    private void MergeBaseMap(TypeMap map, TypeMap baseMap)
    {
        foreach (var basePropertyMap in baseMap.PropertyMaps)
        {
            var existing = map.PropertyMaps.FirstOrDefault(p => p.DestinationProperty.Name == basePropertyMap.DestinationProperty.Name);
            if (existing == null)
            {
                map.PropertyMaps.Add(ClonePropertyMap(basePropertyMap));
                continue;
            }

            MergePropertyMap(existing, basePropertyMap);
        }

        if (baseMap.BeforeMapAction != null)
        {
            map.BeforeMapAction = ComposeActions(baseMap.BeforeMapAction, map.BeforeMapAction);
        }

        if (baseMap.AfterMapAction != null)
        {
            map.AfterMapAction = ComposeActions(baseMap.AfterMapAction, map.AfterMapAction);
        }
    }

    private static PropertyMap ClonePropertyMap(PropertyMap propertyMap)
    {
        return new PropertyMap(propertyMap.DestinationProperty)
        {
            SourceProperty = propertyMap.SourceProperty,
            SourceExpression = propertyMap.SourceExpression,
            Ignore = propertyMap.Ignore,
            PreCondition = propertyMap.PreCondition,
            Condition = propertyMap.Condition,
            ValueConverter = propertyMap.ValueConverter,
            ValueConverterSourceType = propertyMap.ValueConverterSourceType
        };
    }

    private static void MergePropertyMap(PropertyMap target, PropertyMap source)
    {
        if (!target.Ignore
            && target.SourceProperty == null
            && target.SourceExpression == null
            && target.PreCondition == null
            && target.Condition == null
            && target.ValueConverter == null)
        {
            target.Ignore = source.Ignore;
        }

        target.SourceProperty ??= source.SourceProperty;
        target.SourceExpression ??= source.SourceExpression;
        target.PreCondition ??= source.PreCondition;
        target.Condition ??= source.Condition;
        target.ValueConverter ??= source.ValueConverter;
        target.ValueConverterSourceType ??= source.ValueConverterSourceType;
    }

    private static Action<object, object> ComposeActions(Action<object, object> first, Action<object, object>? second)
    {
        return second == null
            ? first
            : (src, dest) =>
            {
                first(src, dest);
                second(src, dest);
            };
    }

    private PropertyInfo? ResolveReverseTargetProperty(TypeMap forwardMap, PropertyMap propertyMap)
    {
        return GetDirectSourceProperty(propertyMap.SourceExpression)
               ?? propertyMap.SourceProperty
               ?? TryResolveSourceProperty(forwardMap.SourceType, propertyMap.DestinationProperty);
    }

    private static LambdaExpression? BuildReverseSourceExpression(Type reverseSourceType, PropertyInfo sourceProperty)
    {
        var sourceAccessor = reverseSourceType.GetProperty(sourceProperty.Name, BindingFlags.Public | BindingFlags.Instance);
        if (sourceAccessor == null || !sourceAccessor.CanRead)
        {
            return null;
        }

        var sourceParameter = Expression.Parameter(reverseSourceType, "src");
        var body = Expression.Property(sourceParameter, sourceAccessor);
        return Expression.Lambda(body, sourceParameter);
    }

    private static PropertyInfo? GetDirectSourceProperty(LambdaExpression? sourceExpression)
    {
        if (sourceExpression?.Parameters.Count != 1)
        {
            return null;
        }

        var body = UnwrapConversion(sourceExpression.Body);
        if (body is MemberExpression member
            && member.Member is PropertyInfo property
            && member.Expression == sourceExpression.Parameters[0])
        {
            return property;
        }

        return null;
    }

    private PropertyMap FindOrAddPropertyMap(TypeMap map, PropertyInfo destinationProperty)
    {
        var propertyMap = map.PropertyMaps.FirstOrDefault(p => p.DestinationProperty == destinationProperty)
            ?? map.PropertyMaps.FirstOrDefault(p => p.DestinationProperty.Name == destinationProperty.Name);

        if (propertyMap != null)
        {
            return propertyMap;
        }

        var created = new PropertyMap(destinationProperty);
        map.PropertyMaps.Add(created);
        return created;
    }

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

    private bool TryMatchByStrategy(PropertyInfo destProp, IEnumerable<PropertyInfo> sourceProps, out PropertyInfo? matched)
    {
        foreach (var srcProp in sourceProps)
        {
            foreach (var strategy in MemberMatchingStrategies)
            {
                if (strategy(srcProp, destProp))
                {
                    matched = srcProp;
                    return true;
                }
            }
        }

        matched = null;
        return false;
    }

    private PropertyInfo? TryResolveSourceProperty(Type sourceType, PropertyInfo destinationProperty)
    {
        var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);

        if (TryApplyAttributeLookup(destinationProperty, sourceProperties, out var attributed))
        {
            return attributed;
        }

        if (TryMatchByStrategy(destinationProperty, sourceProperties.Values, out var strategyProp))
        {
            return strategyProp;
        }

        var normalizedDestinationName = DestinationNamingConvention.Normalize(destinationProperty.Name);
        return sourceProperties.Values.FirstOrDefault(p =>
            string.Equals(SourceNamingConvention.Normalize(p.Name), normalizedDestinationName, StringComparison.Ordinal));
    }

    private static bool TryApplyAttributeLookup(PropertyInfo destinationProperty, IDictionary<string, PropertyInfo> sourceProperties, out PropertyInfo? sourceProperty)
    {
        var mapFrom = destinationProperty.GetCustomAttribute<MapFromAttribute>();
        if (mapFrom != null && sourceProperties.TryGetValue(mapFrom.SourceMember, out sourceProperty))
        {
            return true;
        }

        sourceProperty = null;
        return false;
    }

    private bool TryApplyAttributes(PropertyMap propertyMap, IDictionary<string, PropertyInfo> srcProps)
    {
        var destProp = propertyMap.DestinationProperty;
        var ignoreAttr = destProp.GetCustomAttribute<IgnoreMapAttribute>();
        if (ignoreAttr != null)
        {
            propertyMap.Ignore = true;
            return true;
        }

        var mapFrom = destProp.GetCustomAttribute<MapFromAttribute>();
        if (mapFrom != null)
        {
            if (srcProps.TryGetValue(mapFrom.SourceMember, out var sourceProp))
            {
                propertyMap.SourceProperty = sourceProp;
            }
            return true;
        }

        return false;
    }

    private static Expression UnwrapConversion(Expression expression)
    {
        while (expression.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
        {
            expression = ((UnaryExpression)expression).Operand;
        }

        return expression;
    }
}
