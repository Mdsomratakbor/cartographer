using System;
using System.Linq;
using System.Reflection;
using Cartographer.Core.Abstractions;
using Cartographer.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cartographer.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCartographer(this IServiceCollection services, params Assembly[] assembliesWithProfiles)
    {
        return services.AddCartographer(cfg => ApplyProfiles(cfg, assembliesWithProfiles));
    }

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
