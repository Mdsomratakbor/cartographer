using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Cartographer.Core.Configuration;

public class PropertyMap
{
    public PropertyMap(PropertyInfo destinationProperty)
    {
        DestinationProperty = destinationProperty;
    }

    public PropertyInfo DestinationProperty { get; }
    public PropertyInfo? SourceProperty { get; set; }
    public LambdaExpression? SourceExpression { get; set; }
    public bool Ignore { get; set; }
}
