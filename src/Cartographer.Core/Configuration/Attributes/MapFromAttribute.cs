using System;

namespace Cartographer.Core.Configuration.Attributes;

/// <summary>
/// Maps the destination member from a named source member.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MapFromAttribute : Attribute
{
    public MapFromAttribute(string sourceMember)
    {
        SourceMember = sourceMember;
    }

    public string SourceMember { get; }
}
