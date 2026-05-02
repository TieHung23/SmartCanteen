using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using System.Security.Claims;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using NpgsqlTypes;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SC.Api.DependencyInjection.Configurations;

public static class Configurations
{
    public static void ConfigureSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();

        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new HeaderApiVersionReader("X-Api-Version");
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = false;
            });

        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<ApiVersionHeaderOperationFilter>();
        });
    }

    public static void ConfigureLogging(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'DefaultConnection' was not found.");

        var tableName = builder.Configuration["Logging:Database:TableName"] ?? "ApplicationLogs";
        var minimumLevel = ParseLogLevel(builder.Configuration["Logging:MinimumLevel"]);

        var columnWriters = new Dictionary<string, ColumnWriterBase>
        {
            ["Message"] = new RenderedMessageColumnWriter(),
            ["MessageTemplate"] = new MessageTemplateColumnWriter(),
            ["Level"] = new LevelColumnWriter(true, NpgsqlDbType.Varchar),
            ["TimeStamp"] = new TimestampColumnWriter(NpgsqlDbType.TimestampTz),
            ["Exception"] = new ExceptionColumnWriter(),
            ["Properties"] = new LogEventSerializedColumnWriter(),
            ["UserId"] = new SinglePropertyColumnWriter("UserId", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["RequestPath"] = new SinglePropertyColumnWriter("RequestPath"),
            ["HttpMethod"] =
                new SinglePropertyColumnWriter("HttpMethod", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar)
        };

        builder.Host.UseSerilog((_, _, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Is(minimumLevel)
                .Enrich.FromLogContext()
                .WriteTo.Console(restrictedToMinimumLevel: minimumLevel)
                .WriteTo.Map("LogDate", "unknown-date", (logDate, writeToByDate) =>
                    writeToByDate.Map("UserId", "anonymous", (userId, writeToByUser) =>
                        writeToByUser.File(
                            $"Logs/{SanitizePathSegment(logDate)}/{SanitizePathSegment(userId)}/log-.txt",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 30,
                            shared: true,
                            restrictedToMinimumLevel: minimumLevel)))
                .WriteTo.PostgreSQL(
                    connectionString,
                    tableName,
                    columnWriters,
                    needAutoCreateTable: false);
        });
    }

    public static void UseRequestLogEnrichment(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            using (LogContext.PushProperty("LogDate", DateTime.UtcNow.ToString("ddMMyyyy")))
            using (LogContext.PushProperty("UserId", ResolveUserId(context)))
            using (LogContext.PushProperty("RequestPath", context.Request.Path.Value ?? string.Empty))
            using (LogContext.PushProperty("HttpMethod", context.Request.Method))
            {
                await next();
            }
        });
    }

    private static LogEventLevel ParseLogLevel(string? value)
    {
        return Enum.TryParse<LogEventLevel>(value, true, out var level)
            ? level
            : LogEventLevel.Information;
    }

    private static string ResolveUserId(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? context.User.FindFirstValue("sub")
                     ?? "anonymous";

        return SanitizePathSegment(userId);
    }

    private static string SanitizePathSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "unknown";

        var invalidChars = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());

        return cleaned.Trim();
    }
}

public sealed class ConfigureSwaggerOptions(
    IApiVersionDescriptionProvider provider,
    IConfiguration configuration)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        var swaggerTitle = configuration["Swagger:Title"] ?? "SmartCanteen API";

        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = swaggerTitle,
                Version = description.GroupName,
                Description = description.IsDeprecated
                    ? "This API version has been deprecated."
                    : null
            });
        }
    }
}

public sealed class ApiVersionHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];

        if (operation.Parameters.Any(parameter =>
                string.Equals(parameter.Name, "X-Api-Version", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Api-Version",
            In = ParameterLocation.Header,
            Required = false,
            Description = "API version. Defaults to 1.0.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Default = new OpenApiString("1.0")
            }
        });
    }
}