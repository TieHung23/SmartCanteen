using System.Reflection;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace SC.Application.DependencyInjection.Configurations;

public static class Configurations
{
    public static IServiceCollection AddApplicationConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        var mediatRSection = configuration.GetSection(MediatROptions.SectionName);
        services.Configure<MediatROptions>(mediatRSection);
        var licenseKey = mediatRSection[nameof(MediatROptions.LicenseKey)];

        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = licenseKey ?? string.Empty;
            cfg.RegisterServicesFromAssembly(Assembly.Get);
        });

        return services;
    }
}