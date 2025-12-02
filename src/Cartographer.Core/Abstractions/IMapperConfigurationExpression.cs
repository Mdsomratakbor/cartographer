using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Cartographer.Core.Configuration.Converters;
using Cartographer.Core.Configuration;
using Cartographer.Core.Configuration.Naming;

namespace Cartographer.Core.Abstractions;

/// <summary>
/// Configures mappings between source and destination types.
/// </summary>
public interface IMapperConfigurationExpression
{
    /// <summary>
    /// Naming convention applied to source members when matching.
    /// </summary>
    INamingConvention SourceNamingConvention { get; set; }

    /// <summary>
    /// Naming convention applied to destination members when matching.
    /// </summary>
    INamingConvention DestinationNamingConvention { get; set; }

    /// <summary>
    /// Additional strategies for matching members beyond naming conventions.
    /// </summary>
    IList<Func<PropertyInfo, PropertyInfo, bool>> MemberMatchingStrategies { get; }

    /// <summary>
    /// Maximum mapping depth; when exceeded nested mapping yields null/default.
    /// </summary>
    int? MaxDepth { get; set; }

    /// <summary>
    /// Whether to preserve references when mapping object graphs.
    /// </summary>
    bool PreserveReferences { get; set; }

    /// <summary>
    /// How null source collections are handled globally.
    /// </summary>
    NullCollectionStrategy NullCollectionStrategy { get; set; }

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

    /// <summary>
    /// Uses a type converter to map from source to destination, bypassing member-by-member mapping.
    /// </summary>
    ITypeMapExpression<TSource, TDestination> ConvertUsing(ITypeConverter<TSource, TDestination> converter);

    /// <summary>
    /// Adds a hook executed before member mapping occurs.
    /// </summary>
    ITypeMapExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> action);

    /// <summary>
    /// Adds a hook executed after member mapping completes.
    /// </summary>
    ITypeMapExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> action);
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

    /// <summary>
    /// Maps the member only when the condition is true.
    /// </summary>
    void Condition(Expression<Func<TSource, bool>> predicate);

    /// <summary>
    /// Evaluates before mapping begins; when false the member is skipped.
    /// </summary>
    void PreCondition(Expression<Func<TSource, bool>> predicate);

    /// <summary>
    /// Uses a value converter to populate the destination member.
    /// </summary>
    void ConvertUsing<TSourceMember>(IValueConverter<TSourceMember, TMember> converter, Expression<Func<TSource, TSourceMember>>? sourceMember = null);
}
