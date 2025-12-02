using System;
using System.Linq.Expressions;
using Cartographer.Core.Abstractions;

namespace Cartographer.Core.Configuration;

internal class MemberConfigurationExpression<TSource, TDestination, TMember> : IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    private readonly PropertyMap _propertyMap;

    public MemberConfigurationExpression(PropertyMap propertyMap)
    {
        _propertyMap = propertyMap;
    }

    public void MapFrom(Expression<Func<TSource, TMember>> sourceMember)
    {
        _propertyMap.SourceExpression = sourceMember;
    }

    public void Ignore()
    {
        _propertyMap.Ignore = true;
    }

    public void Condition(Expression<Func<TSource, bool>> predicate)
    {
        _propertyMap.Condition = predicate;
    }

    public void PreCondition(Expression<Func<TSource, bool>> predicate)
    {
        _propertyMap.PreCondition = predicate;
    }
}
