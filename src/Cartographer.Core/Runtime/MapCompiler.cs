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
            var compiled = Compile(map);
            map.MappingFunc = compiled.CreateFunc;
            map.UpdateAction = compiled.UpdateAction;
        }
    }

    private (Func<object, IMapper, object> CreateFunc, Action<object, object, IMapper> UpdateAction) Compile(TypeMap map)
    {
        var sourceObj = Expression.Parameter(typeof(object), "source");
        var mapperParam = Expression.Parameter(typeof(IMapper), "mapper");
        var mapsConst = Expression.Constant(_maps);

        var srcTyped = Expression.Variable(map.SourceType, "src");
        var destTyped = Expression.Variable(map.DestinationType, "dest");

        var mapValueMethod = typeof(MapCompiler).GetMethod(nameof(MapValue), BindingFlags.NonPublic | BindingFlags.Static)
                              ?? throw new InvalidOperationException("Missing MapValue helper.");

        if (map.TypeConverter != null)
        {
            return CompileTypeConverter(map, sourceObj, mapperParam, srcTyped);
        }

        var createExpressions = new List<Expression>
        {
            Expression.Assign(srcTyped, Expression.Convert(sourceObj, map.SourceType)),
            Expression.Assign(destTyped, Expression.New(map.DestinationType))
        };

        if (map.BeforeMapAction != null)
        {
            createExpressions.Add(Expression.Invoke(Expression.Constant(map.BeforeMapAction), srcTyped, destTyped));
        }

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

            var (assignment, _) = BuildMemberAssignment(propertyMap, srcTyped, mapperParam, mapsConst, mapValueMethod, destTyped);

            Expression guardedAssignment = assignment;

            if (propertyMap.PreCondition != null)
            {
                guardedAssignment = Expression.IfThen(
                    Expression.Invoke(propertyMap.PreCondition, srcTyped),
                    guardedAssignment);
            }

            if (propertyMap.Condition != null)
            {
                guardedAssignment = Expression.IfThen(
                    Expression.Invoke(propertyMap.Condition, srcTyped),
                    guardedAssignment);
            }

            createExpressions.Add(guardedAssignment);
        }

        if (map.AfterMapAction != null)
        {
            createExpressions.Add(Expression.Invoke(Expression.Constant(map.AfterMapAction), srcTyped, destTyped));
        }

        createExpressions.Add(Expression.Convert(destTyped, typeof(object)));

        var createBody = Expression.Block(new[] { srcTyped, destTyped }, createExpressions);

        var createLambda = Expression.Lambda<Func<object, IMapper, object>>(createBody, sourceObj, mapperParam);

        // Update action (map into existing destination)
        var destObj = Expression.Parameter(typeof(object), "destination");
        var destTypedUpdate = Expression.Variable(map.DestinationType, "destUpdate");
        var updateExpressions = new List<Expression>
        {
            Expression.Assign(srcTyped, Expression.Convert(sourceObj, map.SourceType)),
            Expression.Assign(destTypedUpdate, Expression.Convert(destObj, map.DestinationType))
        };

        if (map.BeforeMapAction != null)
        {
            updateExpressions.Add(Expression.Invoke(Expression.Constant(map.BeforeMapAction), srcTyped, destTypedUpdate));
        }

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

            var (assign, _) = BuildMemberAssignment(propertyMap, srcTyped, mapperParam, mapsConst, mapValueMethod, destTypedUpdate);

            Expression guardedAssignment = assign;

            if (propertyMap.PreCondition != null)
            {
                guardedAssignment = Expression.IfThen(
                    Expression.Invoke(propertyMap.PreCondition, srcTyped),
                    guardedAssignment);
            }

            if (propertyMap.Condition != null)
            {
                guardedAssignment = Expression.IfThen(
                    Expression.Invoke(propertyMap.Condition, srcTyped),
                    guardedAssignment);
            }

            updateExpressions.Add(guardedAssignment);
        }

        if (map.AfterMapAction != null)
        {
            updateExpressions.Add(Expression.Invoke(Expression.Constant(map.AfterMapAction), srcTyped, destTypedUpdate));
        }

        var updateBody = Expression.Block(new[] { srcTyped, destTypedUpdate }, updateExpressions);
        var updateLambda = Expression.Lambda<Action<object, object, IMapper>>(updateBody, sourceObj, destObj, mapperParam);

        return (createLambda.Compile(), updateLambda.Compile());
    }

    private (Func<object, IMapper, object> CreateFunc, Action<object, object, IMapper> UpdateAction) CompileTypeConverter(TypeMap map, ParameterExpression sourceObj, ParameterExpression mapperParam, ParameterExpression srcTyped)
    {
        var converterObj = Expression.Constant(map.TypeConverter);
        var converterType = map.TypeConverter!.GetType();
        var convertMethod = converterType.GetMethod("Convert");
        if (convertMethod == null)
        {
            throw new InvalidOperationException("Type converter must have a Convert method.");
        }

        var createExpressions = new List<Expression>
        {
            Expression.Assign(srcTyped, Expression.Convert(sourceObj, map.SourceType)),
            Expression.Call(converterObj, convertMethod, Expression.Convert(sourceObj, map.SourceType))
        };

        var createBody = Expression.Block(new[] { srcTyped }, createExpressions);
        var createLambda = Expression.Lambda<Func<object, IMapper, object>>(Expression.Convert(createBody, typeof(object)), sourceObj, mapperParam);

        // Update: convert and copy properties to existing destination
        var destObj = Expression.Parameter(typeof(object), "destination");
        var destTypedUpdate = Expression.Variable(map.DestinationType, "destUpdate");
        var converterCall = Expression.Call(converterObj, convertMethod, Expression.Convert(sourceObj, map.SourceType));
        var tempResult = Expression.Variable(map.DestinationType, "converted");

        var updateExpressions = new List<Expression>
        {
            Expression.Assign(srcTyped, Expression.Convert(sourceObj, map.SourceType)),
            Expression.Assign(destTypedUpdate, Expression.Convert(destObj, map.DestinationType)),
            Expression.Assign(tempResult, Expression.Convert(converterCall, map.DestinationType))
        };

        foreach (var prop in map.DestinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite && p.CanRead))
        {
            updateExpressions.Add(
                Expression.Assign(Expression.Property(destTypedUpdate, prop), Expression.Property(tempResult, prop)));
        }

        var updateBody = Expression.Block(new[] { srcTyped, destTypedUpdate, tempResult }, updateExpressions);
        var updateLambda = Expression.Lambda<Action<object, object, IMapper>>(updateBody, sourceObj, destObj, mapperParam);

        return (createLambda.Compile(), updateLambda.Compile());
    }

    private (Expression Assignment, Type SourceValueType) BuildMemberAssignment(PropertyMap propertyMap, Expression srcTyped, Expression mapperParam, Expression mapsConst, MethodInfo mapValueMethod, Expression destTyped)
    {
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

        Expression valueExpression;

        if (propertyMap.ValueConverter != null)
        {
            var converterConst = Expression.Constant(propertyMap.ValueConverter);
            var convertMethod = propertyMap.ValueConverter.GetType().GetMethod("Convert");
            if (convertMethod == null)
            {
                throw new InvalidOperationException("Value converter must have a Convert method.");
            }

            valueExpression = Expression.Call(converterConst, convertMethod, Expression.Convert(sourceValue, propertyMap.ValueConverterSourceType ?? sourceValueType));
        }
        else
        {
            var callMapValue = Expression.Call(
                mapValueMethod,
                Expression.Convert(sourceValue, typeof(object)),
                Expression.Constant(sourceValueType, typeof(Type)),
                Expression.Constant(propertyMap.DestinationProperty.PropertyType, typeof(Type)),
                mapperParam,
                mapsConst);

            valueExpression = Expression.Convert(callMapValue, propertyMap.DestinationProperty.PropertyType);
        }

        var assignment = Expression.Assign(
            Expression.Property(destTyped, propertyMap.DestinationProperty),
            valueExpression);

        return (assignment, sourceValueType);
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
