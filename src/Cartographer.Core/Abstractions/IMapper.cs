using System;

namespace Cartographer.Core.Abstractions;

/// <summary>
/// Executes mapping operations using the configured type maps.
/// </summary>
public interface IMapper
{
    /// <summary>
    /// Maps the given source object to a new destination instance of <typeparamref name="TDestination"/>.
    /// </summary>
    /// <param name="source">Source object to map from.</param>
    /// <returns>A new destination instance populated from the source.</returns>
    TDestination Map<TDestination>(object source);

    /// <summary>
    /// Maps the given source object to a new destination instance of <paramref name="destinationType"/>.
    /// </summary>
    /// <param name="source">Source object to map from.</param>
    /// <param name="sourceType">Runtime type of the source.</param>
    /// <param name="destinationType">Destination type to map to.</param>
    /// <returns>A new destination instance populated from the source.</returns>
    object Map(object source, Type sourceType, Type destinationType);

    /// <summary>
    /// Maps the given source object into the provided destination instance.
    /// </summary>
    /// <param name="source">Source object to map from.</param>
    /// <param name="destination">Existing destination instance to populate.</param>
    /// <typeparam name="TDestination">Destination type.</typeparam>
    /// <returns>The same destination instance after mapping.</returns>
    TDestination Map<TDestination>(object source, TDestination destination);
}
