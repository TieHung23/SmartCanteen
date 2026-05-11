using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using SC.Api.Middleware;
using SC.Application.DependencyInjection.Configurations;
using SC.Contract.DependencyInjection.Configurations;
using SC.Infrastructure.DependencyInjection.Configurations;
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