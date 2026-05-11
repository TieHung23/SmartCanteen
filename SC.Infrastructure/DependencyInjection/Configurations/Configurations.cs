using CloudinaryDotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SC.Infrastructure.BackgroundServices;
using SC.Infrastructure.DependencyInjection.Options;
using SC.Infrastructure.Services.Cloudinary;

namespace SC.Infrastructure.DependencyInjection.Configurations;

public static class Configurations
{
    public static IServiceCollection AddInfrastructureConfigurations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CloundinaryOptions>(configuration.GetSection(CloundinaryOptions.SectionName));
        services.Configure<LogUploadOptions>(configuration.GetSection(LogUploadOptions.SectionName));

        var cloudName = configuration[$"{CloundinaryOptions.SectionName}:CloudName"];
        var apiKey = configuration[$"{CloundinaryOptions.SectionName}:ApiKey"];
        var apiSecret = configuration[$"{CloundinaryOptions.SectionName}:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName)
            || string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(apiSecret))
        {
            services.AddScoped<ICloundinaryUpload, NoOpCloundinaryUpload>();
        }
        else
        {
            services.AddSingleton(_ => new Cloudinary(new Account(cloudName, apiKey, apiSecret)));
            services.AddScoped<ICloundinaryUpload, CloundinaryUpload>();
        }

        services.AddHostedService<DailyLogUploadBackgroundService>();
        
        services.AddScoped<SC.Infrastructure.Services.ApiLog.IApiLogService, SC.Infrastructure.Services.ApiLog.ApiLogService>();

        return services;
    }
}
