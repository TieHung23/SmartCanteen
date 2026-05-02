using Microsoft.Extensions.DependencyInjection;
using SC.Contract.Services.HealthCheck;

namespace SC.Contract.DependencyInjection.Configurations;

public static class Configurations
{
    public static IServiceCollection AddContractConfigurations(this IServiceCollection services)
    {
        services.AddScoped<IHealthCheckService, HealthCheckService>();

        return services;
    }
}