using System;
using System.Collections.Generic;

namespace Cartographer.Core.Configuration;

/// <summary>
/// Thrown when mapper configuration validation fails.
/// </summary>
public class ConfigurationValidationException : Exception
{
    public ConfigurationValidationException(IReadOnlyCollection<string> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors;
    }

    /// <summary>
    /// The collection of validation error messages.
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; }

    private static string BuildMessage(IReadOnlyCollection<string> errors)
    {
        return $"Mapper configuration is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}";
    }
}
