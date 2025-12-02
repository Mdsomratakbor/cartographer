using System;
using System.Collections.Generic;
using Cartographer.Core.Abstractions;

namespace Cartographer.Core.Configuration;

public class TypeMap
{
    public TypeMap(Type sourceType, Type destinationType)
    {
        SourceType = sourceType;
        DestinationType = destinationType;
    }

    public Type SourceType { get; }
    public Type DestinationType { get; }
    public List<PropertyMap> PropertyMaps { get; } = new();
    internal Func<object, IMapper, object>? MappingFunc { get; set; }
}
