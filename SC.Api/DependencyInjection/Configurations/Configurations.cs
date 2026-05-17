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
    public static void ConfigureCors(this IServiceCollection services, CorsOptions corsOptions)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                if (corsOptions.AllowedOrigins is { Length: > 0 })
                {
                    builder.WithOrigins(corsOptions.AllowedOrigins);
                }
                else
                {
                    builder.AllowAnyOrigin();
                }

                if (corsOptions.AllowedMethods is { Length: > 0 })
                {
                    builder.WithMethods(corsOptions.AllowedMethods);
                }
                else
                {
                    builder.AllowAnyMethod();
                }

                if (corsOptions.AllowedHeaders is { Length: > 0 })
                {
                    builder.WithHeaders(corsOptions.AllowedHeaders);
                }
                else
                {
                    builder.AllowAnyHeader();
                }
            });
        });
    }

    public static void ConfigureRateLimiter(this IServiceCollection services, RateLimitOptions rateLimitOptions)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("FixedWindowPolicy", httpContext =>
                System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.Window),
                        QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                        QueueLimit = rateLimitOptions.QueueLimit
                    }));
            
            // Set the default policy to the fixed window policy
            options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.Window),
                        QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                        QueueLimit = rateLimitOptions.QueueLimit
                    }));
        });
    }
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

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the JWT access token returned by /api/auth/login."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    public static void ConfigureLogging(this WebApplicationBuilder builder, LoggingOptions loggingOptions, string connectionString)
    {
        var tableName = NormalizeTableName(loggingOptions.Database.TableName);
        var minimumLevel = ParseLogLevel(loggingOptions.MinimumLevel);
        var columnWriters = new Dictionary<string, ColumnWriterBase>
        {
            ["\"LoginId\""] = new SinglePropertyColumnWriter("LoginId", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["\"LogLevel\""] = new LevelColumnWriter(true, NpgsqlDbType.Varchar),
            ["\"ApiUrl\""] = new SinglePropertyColumnWriter("ApiUrl", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["\"ApiMethod\""] = new SinglePropertyColumnWriter("ApiMethod", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["\"Message\""] = new RenderedMessageColumnWriter(),
            ["\"ErrorTrace\""] = new ExceptionColumnWriter(),
            ["\"ApiBody\""] = new SinglePropertyColumnWriter("ApiBody", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["\"ApiResponse\""] = new SinglePropertyColumnWriter("ApiResponse", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["\"LocalIpAddress\""] = new SinglePropertyColumnWriter("LocalIpAddress", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["\"LocalHostPC\""] = new SinglePropertyColumnWriter("LocalHostPC", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["\"LogApp\""] = new SinglePropertyColumnWriter("LogApp", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["\"LogVersion\""] = new SinglePropertyColumnWriter("LogVersion", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["\"Memo\""] = new SinglePropertyColumnWriter("Memo", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["\"RequestId\""] = new SinglePropertyColumnWriter("RequestId", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["\"ApiDesc\""] = new SinglePropertyColumnWriter("ApiDesc", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["\"ApiVer\""] = new SinglePropertyColumnWriter("ApiVer", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["\"CreatedDate\""] = new TimestampColumnWriter(NpgsqlDbType.TimestampTz),
            ["\"EndDate\""] = new SinglePropertyColumnWriter("EndDate", PropertyWriteMethod.ToString, NpgsqlDbType.TimestampTz)
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
                                    restrictedToMinimumLevel: minimumLevel))));
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