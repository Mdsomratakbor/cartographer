using System;

namespace Cartographer.Core.Configuration.Naming;

/// <summary>
/// Normalizes member names so they can be compared across differing naming styles.
/// </summary>
public interface INamingConvention
{
    string Normalize(string name);
}

/// <summary>
/// Does not alter the member name.
/// </summary>
public sealed class IdentityNamingConvention : INamingConvention
{
    public string Normalize(string name) => name;
}

/// <summary>
/// Treats names as snake_case when normalizing.
/// </summary>
public sealed class SnakeCaseNamingConvention : INamingConvention
{
    public string Normalize(string name) => StripSeparators(name);

    private static string StripSeparators(string name)
    {
        return name.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
    }
}

/// <summary>
/// Treats names as PascalCase when normalizing.
/// </summary>
public sealed class PascalCaseNamingConvention : INamingConvention
{
    public string Normalize(string name) => StripSeparators(name);

    private static string StripSeparators(string name)
    {
        return name.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
    }
}
