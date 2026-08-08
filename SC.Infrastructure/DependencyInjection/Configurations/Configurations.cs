using CloudinaryDotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SC.Contract.Services.Auth;
using SC.Contract.Services.Email;
using SC.Contract.Services.Payment;
using SC.Contract.Services.Storage;
using SC.Contract.Services.Verification;
using SC.Contract.Services.Notification;
using SC.Infrastructure.BackgroundServices;
using SC.Infrastructure.DependencyInjection.Options;
using SC.Infrastructure.Services.Auth;
using SC.Infrastructure.Services.Cloudinary;
using SC.Infrastructure.Services.Cache;
using SC.Infrastructure.Services.Email;
using SC.Infrastructure.Services.Payment;
using SC.Infrastructure.Services.Storage;
using SC.Infrastructure.Services.Verification;
using SC.Infrastructure.Services.Notification;
using SC.Domain.SharedKernel;

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
        services.Configure<SePayOptions>(configuration.GetSection(SePayOptions.SectionName));
        services
            .AddOptions<FirebaseOptions>()
            .Bind(configuration.GetSection(FirebaseOptions.SectionName))
            .Validate(
                options => !options.Enabled
                           || ((!string.IsNullOrWhiteSpace(options.ServiceAccountPath)
                                || !string.IsNullOrWhiteSpace(options.ServiceAccountJson))
                               && !string.IsNullOrWhiteSpace(options.ApiBaseUrl)
                               && Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out _)
                               && !string.IsNullOrWhiteSpace(options.MessagingScope)),
                "Firebase service account, API base URL, and messaging scope must be configured when Firebase is enabled.")
            .ValidateOnStart();
        services
            .AddOptions<NotificationTemplateOptions>()
            .Bind(configuration.GetSection(NotificationTemplateOptions.SectionName))
            .Validate(
                options => options.Templates.Count > 0
                           && options.Templates.Values.All(IsValidTemplate),
                "At least one complete notification template must be configured.")
            .ValidateOnStart();

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddSingleton<ISePayWebhookVerifier, SePayWebhookVerifier>();
        services.AddScoped<IFileValidator, FileValidator>();
        services.AddScoped<IFileUploader, CloudinaryFileUploaderAdapter>();
        services.AddSingleton<INotificationTemplateProvider, NotificationTemplateProvider>();
        services.AddHttpClient<INotificationPushPublisher, FirebaseNotificationPushPublisher>();

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
        services.AddHostedService<SessionFinalizationJob>();
        services.AddHostedService<ChangeProposalExpirationJob>();
        services.AddHostedService<ServingJobWatchdogJob>();
        
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

    private static bool IsSafeActionUrl(string? actionUrl)
    {
        if (string.IsNullOrWhiteSpace(actionUrl))
        {
            return true;
        }

        var value = actionUrl.Trim();
        return value.StartsWith('/')
               && !value.StartsWith("//", StringComparison.Ordinal)
               && !value.Any(char.IsControl);
    }

    private static bool IsValidTemplate(NotificationTemplateDefinition template)
    {
        return !string.IsNullOrWhiteSpace(template.Type)
               && template.Type.Trim().Length <= NotificationConstraints.TypeMaxLength
               && template.Type.All(character =>
                   char.IsLetterOrDigit(character)
                   || character is '_' or '.' or '-')
               && !string.IsNullOrWhiteSpace(template.Title)
               && template.Title.Trim().Length <= NotificationConstraints.TitleMaxLength
               && !string.IsNullOrWhiteSpace(template.MessageTemplate)
               && template.MessageTemplate.Trim().Length <= NotificationConstraints.MessageMaxLength
               && (string.IsNullOrWhiteSpace(template.ReferenceType)
                   || template.ReferenceType.Trim().Length
                   <= NotificationConstraints.ReferenceTypeMaxLength)
               && (string.IsNullOrWhiteSpace(template.ActionUrlTemplate)
                   || template.ActionUrlTemplate.Trim().Length
                   <= NotificationConstraints.ActionUrlMaxLength)
               && IsSafeActionUrl(template.ActionUrlTemplate);
    }
}
