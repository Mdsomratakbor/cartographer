using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Cartographer.Core.Configuration;

/// <summary>
/// Describes how a destination property is populated.
/// </summary>
public class PropertyMap
{
    public PropertyMap(PropertyInfo destinationProperty)
    {
        DestinationProperty = destinationProperty;
    }

    /// <summary>
    /// Destination property being assigned.
    /// </summary>
    public PropertyInfo DestinationProperty { get; }
    /// <summary>
    /// Source property matched by convention.
    /// </summary>
    public PropertyInfo? SourceProperty { get; set; }
    /// <summary>
    /// Custom source expression configured via MapFrom.
    /// </summary>
    public LambdaExpression? SourceExpression { get; set; }
    /// <summary>
    /// Whether this property should be skipped during mapping.
    /// </summary>
    public bool Ignore { get; set; }
}
