using CloudinaryDotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SC.Contract.Services.Auth;
using SC.Contract.Services.Email;
using SC.Contract.Services.Storage;
using SC.Contract.Services.Verification;
using SC.Infrastructure.BackgroundServices;
using SC.Infrastructure.DependencyInjection.Options;
using SC.Infrastructure.Services.Auth;
using SC.Infrastructure.Services.Cloudinary;
using SC.Infrastructure.Services.Cache;
using SC.Infrastructure.Services.Email;
using SC.Infrastructure.Services.Storage;
using SC.Infrastructure.Services.Verification;

namespace SC.Infrastructure.DependencyInjection.Configurations;

public static class Configurations
{
    public static IServiceCollection AddInfrastructureConfigurations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CloundinaryOptions>(configuration.GetSection(CloundinaryOptions.SectionName));
        services.Configure<LogUploadOptions>(configuration.GetSection(LogUploadOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));
        services.Configure<VerificationOptions>(configuration.GetSection(VerificationOptions.SectionName));
        services.Configure<GoogleOptions>(configuration.GetSection(GoogleOptions.SectionName));
        services.Configure<FrontendOptions>(configuration.GetSection(FrontendOptions.SectionName));

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<IFileValidator, FileValidator>();
        services.AddScoped<IFileUploader, CloudinaryFileUploaderAdapter>();

        var resendApiKey = configuration[$"{ResendOptions.SectionName}:ApiKey"];
        if (string.IsNullOrWhiteSpace(resendApiKey))
        {
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        }
        else
        {
            services.AddHttpClient<IEmailSender, ResendEmailSender>();
        }

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

        var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisOptions.ConnectionString;
            options.InstanceName = redisOptions.InstanceName;
        });

        services.AddSingleton<ICacheService, CacheService>();

        return services;
    }
}
