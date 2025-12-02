using System;
using System.Linq.Expressions;

namespace Cartographer.Core.Abstractions;

/// <summary>
/// Configures mappings between source and destination types.
/// </summary>
public interface IMapperConfigurationExpression
{
    /// <summary>
    /// Creates or retrieves a type map between <typeparamref name="TSource"/> and <typeparamref name="TDestination"/>.
    /// </summary>
    ITypeMapExpression<TSource, TDestination> CreateMap<TSource, TDestination>();
}

/// <summary>
/// Fluent API for configuring a map between a source and destination type.
/// </summary>
public interface ITypeMapExpression<TSource, TDestination>
{
    /// <summary>
    /// Creates a reverse map from destination back to source.
    /// </summary>
    ITypeMapExpression<TDestination, TSource> ReverseMap();

    /// <summary>
    /// Configures mapping for a specific destination member.
    /// </summary>
    /// <param name="destMember">Destination member selector.</param>
    /// <param name="memberOptions">Options to control the mapping behavior.</param>
    ITypeMapExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destMember,
        Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions);
}

/// <summary>
/// Fluent options for configuring a destination member map.
/// </summary>
public interface IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    /// <summary>
    /// Specifies a custom source member expression used to populate the destination member.
    /// </summary>
    void MapFrom(Expression<Func<TSource, TMember>> sourceMember);

    /// <summary>
    /// Ignores this destination member during mapping.
    /// </summary>
    void Ignore();
}
