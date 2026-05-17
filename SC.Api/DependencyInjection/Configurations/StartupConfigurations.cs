using System.Text;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SC.Api.Middleware;
using SC.Application.DependencyInjection.Configurations;
using SC.Contract.DependencyInjection.Configurations;
using SC.Infrastructure.DependencyInjection.Configurations;
using SC.Infrastructure.DependencyInjection.Options;
using SC.Persistence.Database;
using SC.Persistence.DependencyInjection.Configurations;
using Serilog;
using SC.Api.DependencyInjection.Options;

namespace SC.Api.DependencyInjection.Configurations;

public static class StartupConfigurations
{
    public static void AddApiConfigurations(this WebApplicationBuilder builder)
    {
        var loggingOptions = builder.Configuration
            .GetSection(LoggingOptions.SectionName)
            .Get<LoggingOptions>() ?? new LoggingOptions();

        builder.Services.Configure<LoggingOptions>(builder.Configuration.GetSection(LoggingOptions.SectionName));

        var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
        builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
        builder.Services.ConfigureCors(corsOptions);

        var rateLimitOptions = builder.Configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();
        builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));
        builder.Services.ConfigureRateLimiter(rateLimitOptions);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'DefaultConnection' was not found.");

        builder.ConfigureLogging(loggingOptions, connectionString);

        builder.Services.ConfigureSwagger(builder.Configuration);
        builder.Services.AddControllers();
        builder.Services.AddScoped<SC.Persistence.Database.Interceptors.AuditableEntityInterceptor>();

        builder.Services.AddDbContext<SmartCanteenDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(SmartCanteenDbContext).Assembly.FullName))
                .AddInterceptors(sp.GetRequiredService<SC.Persistence.Database.Interceptors.AuditableEntityInterceptor>());
        });

        builder.Services.AddScoped<SC.Domain.Abstraction.Services.ICurrentUserService, SC.Api.Services.CurrentUserService>();
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddApplicationConfigurations(builder.Configuration);
        builder.Services.AddContractConfigurations();
        builder.Services.AddFluentValidationConfigurations();
        builder.Services.AddInfrastructureConfigurations(builder.Configuration);
        builder.Services.AddPersistenceConfigurations();

        builder.Services.ConfigureJwtAuthentication(builder.Configuration);
    }

    private static void ConfigureJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwt.SecretKey))
        {
            // No JWT configured — endpoints decorated with [Authorize] will return 401 at runtime.
            // Authentication middleware is still added so [Authorize] attributes resolve.
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            services.AddAuthorization();
            return;
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireVerified", policy =>
                policy.RequireAuthenticatedUser().RequireClaim("verified", "true"));
        });
    }

    public static void UseApiConfigurations(this WebApplication app)
    {
        var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SmartCanteenDbContext>();
            dbContext.Database.Migrate();
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                    $"SmartCanteen API {description.GroupName.ToUpperInvariant()}");
            }

            options.RoutePrefix = "swagger";
        });

        app.UseGlobalExceptionHandler();
        app.UseMiddleware<ApiLoggerMiddleware>();
        app.UseRequestLogEnrichment();
        app.UseRequestResponseBodyLogging();
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                if (httpContext.Items.TryGetValue("RequestBody", out var requestBody))
                {
                    diagnosticContext.Set("RequestBody", requestBody);
                }

                if (httpContext.Items.TryGetValue("ResponseBody", out var responseBody))
                {
                    diagnosticContext.Set("ResponseBody", responseBody);
                }
            };
            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                var path = httpContext.Request.Path.Value;
                if (ex != null || httpContext.Response.StatusCode > 499)
                {
                    return Serilog.Events.LogEventLevel.Error;
                }
                if (path != null && path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
                {
                    return Serilog.Events.LogEventLevel.Debug;
                }
                return Serilog.Events.LogEventLevel.Information;
            };
        });

        app.UseHttpsRedirection();
        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var addresses = app.Services
                .GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>()?
                .Addresses;

            if (addresses is null || addresses.Count == 0)
            {
                app.Logger.LogInformation("Application started, but no server addresses were reported.");
                return;
            }

            foreach (var address in addresses)
            {
                app.Logger.LogInformation("Application listening on {Address}/swagger/index.html", address);
            }
        });
    }
}