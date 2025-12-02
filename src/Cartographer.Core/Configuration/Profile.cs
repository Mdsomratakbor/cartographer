using Cartographer.Core.Abstractions;

namespace Cartographer.Core.Configuration;

/// <summary>
/// Groups related mapping configuration into a reusable profile.
/// </summary>
public abstract class Profile
{
    /// <summary>
    /// Applies this profile's mappings to the provided configuration expression.
    /// </summary>
    public void Apply(IMapperConfigurationExpression cfg) => ConfigureMappings(cfg);

    protected abstract void ConfigureMappings(IMapperConfigurationExpression cfg);
}
