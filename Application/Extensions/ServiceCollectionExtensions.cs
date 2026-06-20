using Microsoft.Extensions.DependencyInjection;
using Predictathon.Application.Attributes;
using Scrutor;
using System.Reflection;

namespace Predictathon.Application.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services necessary to use the application layer.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScopedServices(typeof(ServiceCollectionExtensions).Assembly);
        return services;
    }

    /// <summary>
    /// Registers application services marked with <see cref="ScopedServiceAttribute"/>
    /// as their implemented interfaces with a scoped lifetime.
    /// </summary>
    public static IServiceCollection AddScopedServices(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.Scan(selector => selector
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.Where(type =>
                type.IsDefined(typeof(ScopedServiceAttribute), inherit: true)))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}
