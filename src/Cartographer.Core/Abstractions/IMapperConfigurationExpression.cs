using System;
using System.Linq.Expressions;

namespace Cartographer.Core.Abstractions;

public interface IMapperConfigurationExpression
{
    ITypeMapExpression<TSource, TDestination> CreateMap<TSource, TDestination>();
}

public interface ITypeMapExpression<TSource, TDestination>
{
    ITypeMapExpression<TDestination, TSource> ReverseMap();
    ITypeMapExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destMember,
        Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions);
}

public interface IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    void MapFrom(Expression<Func<TSource, TMember>> sourceMember);
    void Ignore();
}
