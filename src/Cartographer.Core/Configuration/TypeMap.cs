using System;
using System.Collections.Generic;
using Cartographer.Core.Abstractions;
using Cartographer.Core.Runtime;

namespace Cartographer.Core.Configuration;

/// <summary>
/// Describes how to map between a source and destination type.
/// </summary>
public class TypeMap
{
    public TypeMap(Type sourceType, Type destinationType)
    {
        SourceType = sourceType;
        DestinationType = destinationType;
    }

    /// <summary>
    /// Source type for this map.
    /// </summary>
    public Type SourceType { get; }
    /// <summary>
    /// Destination type for this map.
    /// </summary>
    public Type DestinationType { get; }
    /// <summary>
    /// Member-level mapping rules.
    /// </summary>
    public List<PropertyMap> PropertyMaps { get; } = new();
    internal Func<object, IMapper, MappingContext, object>? MappingFunc { get; set; }
    internal Action<object, object, IMapper, MappingContext>? UpdateAction { get; set; }
    internal Action<object, object>? BeforeMapAction { get; set; }
    internal Action<object, object>? AfterMapAction { get; set; }
    internal object? TypeConverter { get; set; }
    internal List<(Type Source, Type Destination)> DerivedTypes { get; } = new();
}
