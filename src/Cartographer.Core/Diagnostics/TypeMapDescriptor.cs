using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cartographer.Core.Diagnostics;

public class TypeMapDescriptor
{
    public TypeMapDescriptor(Type sourceType, Type destinationType, IReadOnlyList<PropertyMapDescriptor> properties)
    {
        SourceType = sourceType;
        DestinationType = destinationType;
        Properties = properties;
    }

    public Type SourceType { get; }
    public Type DestinationType { get; }
    public IReadOnlyList<PropertyMapDescriptor> Properties { get; }
}

public class PropertyMapDescriptor
{
    public PropertyMapDescriptor(PropertyInfo destinationProperty, PropertyInfo? sourceProperty, string? sourceExpression, bool isIgnored)
    {
        DestinationProperty = destinationProperty;
        SourceProperty = sourceProperty;
        SourceExpression = sourceExpression;
        IsIgnored = isIgnored;
    }

    public PropertyInfo DestinationProperty { get; }
    public PropertyInfo? SourceProperty { get; }
    public string? SourceExpression { get; }
    public bool IsIgnored { get; }
}
