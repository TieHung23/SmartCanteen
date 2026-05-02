using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using SC.Api.Middleware;
using SC.Application.DependencyInjection.Configurations;
using SC.Contract.DependencyInjection.Configurations;
using SC.Persistence.Database;
using Serilog;

namespace SC.Api.DependencyInjection.Configurations;

public static class StartupConfigurations
{
    public static void AddApiConfigurations(this WebApplicationBuilder builder)
    {
        builder.ConfigureLogging();

        builder.Services.ConfigureSwagger(builder.Configuration);
        builder.Services.AddControllers();
        builder.Services.AddDbContext<SmartCanteenDbContext>(options =>
        {
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(SmartCanteenDbContext).Assembly.FullName));
        });

        builder.Services.AddApplicationConfigurations();
        builder.Services.AddContractConfigurations();
        builder.Services.AddFluentValidationConfigurations();
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
        app.UseRequestLogEnrichment();
        app.UseSerilogRequestLogging();

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