using System;

namespace Cartographer.Core.Configuration.Attributes;

/// <summary>
/// Indicates the member should be ignored during mapping.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class IgnoreMapAttribute : Attribute
{
}
