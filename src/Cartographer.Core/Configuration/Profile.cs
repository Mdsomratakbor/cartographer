using Cartographer.Core.Abstractions;

namespace Cartographer.Core.Configuration;

public abstract class Profile
{
    public void Apply(IMapperConfigurationExpression cfg) => ConfigureMappings(cfg);

    protected abstract void ConfigureMappings(IMapperConfigurationExpression cfg);
}
