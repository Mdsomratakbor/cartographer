using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;

namespace Cartographer.Core.Runtime;

internal class MapCompiler
{
    private readonly Dictionary<(Type, Type), TypeMap> _maps;

    public MapCompiler(Dictionary<(Type, Type), TypeMap> maps)
    {
        _maps = maps;
    }

    /// <summary>
    /// Compiles mapping delegates for all configured type maps.
    /// </summary>
    public void CompileAll()
    {
        foreach (var map in _maps.Values)
        {
            map.MappingFunc = Compile(map);
        }
    }

    private Func<object, IMapper, object> Compile(TypeMap map)
    {
        var sourceObj = Expression.Parameter(typeof(object), "source");
        var mapperParam = Expression.Parameter(typeof(IMapper), "mapper");
        var mapsConst = Expression.Constant(_maps);

        var srcTyped = Expression.Variable(map.SourceType, "src");
        var destTyped = Expression.Variable(map.DestinationType, "dest");

        var expressions = new List<Expression>
        {
            Expression.Assign(srcTyped, Expression.Convert(sourceObj, map.SourceType)),
            Expression.Assign(destTyped, Expression.New(map.DestinationType))
        };

        var mapValueMethod = typeof(MapCompiler).GetMethod(nameof(MapValue), BindingFlags.NonPublic | BindingFlags.Static)
                              ?? throw new InvalidOperationException("Missing MapValue helper.");

        foreach (var propertyMap in map.PropertyMaps)
        {
            if (propertyMap.Ignore)
            {
                continue;
            }

            if (propertyMap.SourceExpression == null && propertyMap.SourceProperty == null)
            {
                continue;
            }

            Expression sourceValue;
            Type sourceValueType;

            if (propertyMap.SourceExpression != null)
            {
                var invoked = Expression.Invoke(propertyMap.SourceExpression, srcTyped);
                sourceValue = invoked;
                sourceValueType = propertyMap.SourceExpression.ReturnType;
            }
            else
            {
                sourceValue = Expression.Property(srcTyped, propertyMap.SourceProperty!);
                sourceValueType = propertyMap.SourceProperty!.PropertyType;
            }

            var callMapValue = Expression.Call(
                mapValueMethod,
                Expression.Convert(sourceValue, typeof(object)),
                Expression.Constant(sourceValueType, typeof(Type)),
                Expression.Constant(propertyMap.DestinationProperty.PropertyType, typeof(Type)),
                mapperParam,
                mapsConst);

            var assign = Expression.Assign(
                Expression.Property(destTyped, propertyMap.DestinationProperty),
                Expression.Convert(callMapValue, propertyMap.DestinationProperty.PropertyType));

            expressions.Add(assign);
        }

        expressions.Add(Expression.Convert(destTyped, typeof(object)));

        var body = Expression.Block(new[] { srcTyped, destTyped }, expressions);

        var lambda = Expression.Lambda<Func<object, IMapper, object>>(body, sourceObj, mapperParam);
        return lambda.Compile();
    }

    private static Type? GetEnumerableType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
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

    private static object? MapValue(object? value, Type sourceType, Type destinationType, IMapper mapper, Dictionary<(Type, Type), TypeMap> maps)
    {
        if (value == null)
        {
            return null;
        }

        if (destinationType.IsAssignableFrom(sourceType))
        {
            return value;
        }

        if (maps.ContainsKey((sourceType, destinationType)))
        {
            return mapper.Map(value, sourceType, destinationType);
        }

        var sourceElement = GetEnumerableType(sourceType);
        var destinationElement = GetEnumerableType(destinationType);

        if (sourceElement != null && destinationElement != null && typeof(IEnumerable).IsAssignableFrom(sourceType))
        {
            var destListType = typeof(List<>).MakeGenericType(destinationElement);
            var destList = (IList)Activator.CreateInstance(destListType)!;

            foreach (var item in (IEnumerable)value)
            {
                var mappedItem = MapValue(item, sourceElement, destinationElement, mapper, maps);
                destList.Add(mappedItem);
            }

            if (destinationType.IsArray)
            {
                var array = Array.CreateInstance(destinationElement, destList.Count);
                destList.CopyTo(array, 0);
                return array;
            }

            if (destinationType.IsAssignableFrom(destListType))
            {
                return destList;
            }

            var enumerableCtor = destinationType.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(destinationElement) });
            if (enumerableCtor != null)
            {
                return enumerableCtor.Invoke(new object[] { destList });
            }

            var destinationInstance = Activator.CreateInstance(destinationType);
            var addMethod = destinationType.GetMethod("Add", new[] { destinationElement });
            if (destinationInstance != null && addMethod != null)
            {
                foreach (var item in destList)
                {
                    addMethod.Invoke(destinationInstance, new[] { item });
                }

                return destinationInstance;
            }

            return destList;
        }

        return value;
    }
}
