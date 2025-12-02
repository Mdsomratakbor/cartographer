using System;
using System.Linq;
using System.Reflection;
using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cartographer.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Cartographer with profile scanning across provided assemblies (or all loaded assemblies if none supplied).
    /// </summary>
    public static IServiceCollection AddCartographer(this IServiceCollection services, params Assembly[] assembliesWithProfiles)
    {
        return services.AddCartographer(cfg => ApplyProfiles(cfg, assembliesWithProfiles));
    }

    /// <summary>
    /// Registers Cartographer using an explicit configuration action.
    /// </summary>
    public static IServiceCollection AddCartographer(this IServiceCollection services, Action<IMapperConfigurationExpression> configure)
    {
        var config = new MapperConfiguration(configure);
        services.AddSingleton(config);
        services.AddSingleton<IMapper>(_ => config.CreateMapper());
        return services;
    }

    private static void ApplyProfiles(IMapperConfigurationExpression cfg, Assembly[] assemblies)
    {
        var assembliesToScan = (assemblies is { Length: > 0 })
            ? assemblies
            : AppDomain.CurrentDomain.GetAssemblies();

        var profiles = assembliesToScan
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && typeof(Profile).IsAssignableFrom(t))
            .Select(t => (Profile)Activator.CreateInstance(t)!);

        foreach (var profile in profiles)
        {
            profile.Apply(cfg);
        }
    }
}
