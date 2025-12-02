using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Cartographer.Core.Abstractions;

namespace Cartographer.Core.Configuration;

internal class TypeMapExpression<TSource, TDestination> : ITypeMapExpression<TSource, TDestination>
{
    private readonly TypeMap _typeMap;
    private readonly MapperConfiguration _config;

    public TypeMapExpression(TypeMap typeMap, MapperConfiguration config)
    {
        _typeMap = typeMap;
        _config = config;
    }

    public ITypeMapExpression<TDestination, TSource> ReverseMap()
    {
        return _config.CreateMap<TDestination, TSource>();
    }

    public ITypeMapExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> action)
    {
        _typeMap.BeforeMapAction = (src, dest) => action((TSource)src, (TDestination)dest);
        return this;
    }

    public ITypeMapExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> action)
    {
        _typeMap.AfterMapAction = (src, dest) => action((TSource)src, (TDestination)dest);
        return this;
    }

    public ITypeMapExpression<TSource, TDestination> ForMember<TMember>(Expression<Func<TDestination, TMember>> destMember, Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions)
    {
        if (destMember.Body is not MemberExpression me || me.Member is not PropertyInfo destProp)
        {
            throw new InvalidOperationException("ForMember expects a property access.");
        }

        var propertyMap = _typeMap.PropertyMaps.FirstOrDefault(p => p.DestinationProperty == destProp)
                          ?? _typeMap.PropertyMaps.FirstOrDefault(p => p.DestinationProperty.Name == destProp.Name)
                          ?? AddPropertyMap(destProp);

        var memberConfig = new MemberConfigurationExpression<TSource, TDestination, TMember>(propertyMap);
        memberOptions(memberConfig);
        return this;
    }

    private PropertyMap AddPropertyMap(PropertyInfo destProp)
    {
        var pm = new PropertyMap(destProp);
        _typeMap.PropertyMaps.Add(pm);
        return pm;
    }
}
