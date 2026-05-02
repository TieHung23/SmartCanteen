using Microsoft.Extensions.DependencyInjection;

namespace SC.Application.DependencyInjection.Configurations;

public static class Configurations
{
    public static IServiceCollection AddApplicationConfigurations(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Configurations).Assembly));

        return services;
    }
}