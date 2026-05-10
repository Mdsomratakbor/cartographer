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
        return services.AddCartographer(static _ => { }, assembliesWithProfiles);
    }

    /// <summary>
    /// Registers Cartographer using an explicit configuration action and profile scanning.
    /// </summary>
    public static IServiceCollection AddCartographer(this IServiceCollection services, Action<IMapperConfigurationExpression> configure, params Assembly[] assembliesWithProfiles)
    {
        return RegisterCartographer(services, (sp, cfg) =>
        {
            configure(cfg);
            ApplyProfiles(cfg, sp, assembliesWithProfiles);
        });
    }

    /// <summary>
    /// Registers Cartographer using an explicit configuration action.
    /// </summary>
    public static IServiceCollection AddCartographer(this IServiceCollection services, Action<IMapperConfigurationExpression> configure)
    {
        return RegisterCartographer(services, (_, cfg) => configure(cfg));
    }

    private static IServiceCollection RegisterCartographer(IServiceCollection services, Action<IServiceProvider, IMapperConfigurationExpression> configure)
    {
        services.AddSingleton(sp => new MapperConfiguration(cfg => configure(sp, cfg)));
        services.AddSingleton<IMapper>(sp => sp.GetRequiredService<MapperConfiguration>().CreateMapper());
        return services;
    }

    private static void ApplyProfiles(IMapperConfigurationExpression cfg, IServiceProvider serviceProvider, Assembly[] assemblies)
    {
        var assembliesToScan = (assemblies is { Length: > 0 })
            ? assemblies
            : AppDomain.CurrentDomain.GetAssemblies();

        var profiles = assembliesToScan
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && typeof(Profile).IsAssignableFrom(t))
            .Select(t => (Profile)ActivatorUtilities.CreateInstance(serviceProvider, t));

        foreach (var profile in profiles)
        {
            profile.Apply(cfg);
        }
    }
}
