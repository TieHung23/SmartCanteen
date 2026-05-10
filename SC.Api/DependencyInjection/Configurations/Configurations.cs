using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using NpgsqlTypes;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;
using Swashbuckle.AspNetCore.SwaggerGen;
using SC.Api.DependencyInjection.Options;

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

    public static void ConfigureLogging(this WebApplicationBuilder builder, LoggingOptions loggingOptions, string connectionString)
    {
        var tableName = NormalizeTableName(loggingOptions.Database.TableName);
        var minimumLevel = ParseLogLevel(loggingOptions.MinimumLevel);
        var columnWriters = new Dictionary<string, ColumnWriterBase>
        {
            ["\"Message\""] = new RenderedMessageColumnWriter(),
            ["\"MessageTemplate\""] = new MessageTemplateColumnWriter(),
            ["\"Level\""] = new LevelColumnWriter(true, NpgsqlDbType.Varchar),
            ["\"TimeStamp\""] = new TimestampColumnWriter(NpgsqlDbType.TimestampTz),
            ["\"Exception\""] = new ExceptionColumnWriter(),
            ["\"Properties\""] = new LogEventSerializedColumnWriter(),
            ["\"UserId\""] = new SinglePropertyColumnWriter("UserId", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["\"RequestPath\""] = new SinglePropertyColumnWriter("RequestPath"),
            ["\"HttpMethod\""] = new SinglePropertyColumnWriter("HttpMethod", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar)
        };

        builder.Host.UseSerilog((_, _, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Is(minimumLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(restrictedToMinimumLevel: minimumLevel)
                .WriteTo.Logger(logger =>
                    logger
                        .WriteTo.Map("LogDate", "unknown-date", (logDate, writeToByDate) =>
                            writeToByDate.Map("UserId", "anonymous", (userId, writeToByUser) =>
                                writeToByUser.File(
                                    $"Logs/{SanitizePathSegment(logDate)}/{SanitizePathSegment(userId)}/log-.txt",
                                    rollingInterval: RollingInterval.Day,
                                    retainedFileCountLimit: 30,
                                    shared: true,
                                    restrictedToMinimumLevel: minimumLevel))))
                .WriteTo.Logger(logger =>
                    logger
                        .Filter.ByIncludingOnly(IsRequestResponseLog)
                        .WriteTo.PostgreSQL(
                            connectionString,
                            tableName,
                            columnWriters,
                            needAutoCreateTable: false));
        });
    }

    public static void UseRequestLogEnrichment(this WebApplication app)
    {
        app.Use(async (HttpContext context, RequestDelegate next) =>
        {
            using (LogContext.PushProperty("LogDate", DateTime.UtcNow.ToString("ddMMyyyy")))
            using (LogContext.PushProperty("UserId", ResolveUserId(context)))
            using (LogContext.PushProperty("RequestPath", context.Request.Path.Value ?? string.Empty))
            using (LogContext.PushProperty("HttpMethod", context.Request.Method))
            {
                await next(context);
            }
        });
    }

    public static void UseRequestResponseBodyLogging(this WebApplication app)
    {
        app.Use(async (HttpContext context, RequestDelegate next) =>
        {
            var requestBody = await ReadRequestBodyAsync(context.Request);

            var originalBody = context.Response.Body;
            await using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await next(context);

            var responseText = await ReadResponseBodyAsync(context.Response);
            context.Response.Body = originalBody;
            await responseBody.CopyToAsync(originalBody);

            context.Items["RequestBody"] = requestBody;
            context.Items["ResponseBody"] = responseText;
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

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is null or 0)
        {
            return string.Empty;
        }

        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return FormatJsonIfPossible(body, request.ContentType);
    }

    private static async Task<string> ReadResponseBodyAsync(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        return FormatJsonIfPossible(body, response.ContentType);
    }

    private static string FormatJsonIfPossible(string body, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        if (contentType is null || !contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return body;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return body;
        }
    }

    private static string NormalizeTableName(string? tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return "\"ApplicationLogs\"";
        }

        if (tableName.StartsWith("\"") && tableName.EndsWith("\""))
        {
            return tableName;
        }

        var hasUppercase = tableName.Any(char.IsUpper);
        return hasUppercase
            ? $"\"{tableName}\""
            : tableName;
    }

    private static bool IsRequestResponseLog(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
        {
            return false;
        }

        if (sourceContext is ScalarValue scalar && scalar.Value is string source)
        {
            return source.Contains("Serilog.AspNetCore.RequestLoggingMiddleware", StringComparison.Ordinal);
        }

        return false;
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