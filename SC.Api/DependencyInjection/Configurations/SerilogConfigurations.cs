using System.Security.Claims;
using System.Text;
using System.Text.Json;
using NpgsqlTypes;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.PostgreSQL;
using SC.Api.DependencyInjection.Options;

namespace SC.Api.DependencyInjection.Configurations;

public static class SerilogConfigurations
{
    public static void ConfigureSerilog(
        this WebApplicationBuilder builder,
        LoggingOptions loggingOptions,
        string connectionString)
    {
        var tableName = NormalizeTableName(loggingOptions.Database.TableName);
        var minimumLevel = ParseLogLevel(loggingOptions.MinimumLevel);

        // DB column mapping — HTTP request events only
        var columnWriters = new Dictionary<string, ColumnWriterBase>
        {
            ["\"Message\""] = new RenderedMessageColumnWriter(),
            ["\"MessageTemplate\""] = new MessageTemplateColumnWriter(),
            ["\"Level\""] = new LevelColumnWriter(true, NpgsqlDbType.Varchar),
            ["\"TimeStamp\""] = new TimestampColumnWriter(NpgsqlDbType.TimestampTz),
            ["\"Exception\""] = new ExceptionColumnWriter(),
            ["\"Properties\""] = new HttpRequestPropertiesColumnWriter(),
            ["\"UserId\""] = new SinglePropertyColumnWriter("UserId", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar),
            ["\"RequestPath\""] = new SinglePropertyColumnWriter("RequestPath"),
            ["\"HttpMethod\""] = new SinglePropertyColumnWriter("HttpMethod", PropertyWriteMethod.ToString, NpgsqlDbType.Varchar)
        };

        builder.Host.UseSerilog((_, _, cfg) =>
        {
            // ── Minimum level ─────────────────────────────────────────────
            cfg.MinimumLevel.Is(minimumLevel)
               .Enrich.FromLogContext();

            // ── Per-namespace overrides from Serilog:Overrides ────────────
            foreach (var (ns, level) in loggingOptions.Overrides)
            {
                if (Enum.TryParse<LogEventLevel>(level, true, out var overrideLevel))
                    cfg.MinimumLevel.Override(ns, overrideLevel);
            }

            cfg
                // ── Console ─────────────────────────────────────────────────
                //    Plain one-liner; easy to follow in terminal / Docker.
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: minimumLevel)

                // ── File ─────────────────────────────────────────────────────
                //    Scoped to HTTP events AND EF Core commands.
                //    Human-readable: request summary + body/params on separate lines.
                .WriteTo.Logger(sub =>
                    sub.Filter.ByIncludingOnly(e =>
                            e.Properties.ContainsKey("StatusCode") || IsEfCommand(e))
                       .WriteTo.Map("LogDate", "unknown-date", (logDate, byDate) =>
                           byDate.Map("UserId", "anonymous", (userId, byUser) =>
                               byUser.File(
                                   new RequestResponseTextFormatter(),
                                   $"Logs/{SanitizePathSegment(logDate)}/{SanitizePathSegment(userId)}/log-.txt",
                                   rollingInterval: RollingInterval.Day,
                                   retainedFileCountLimit: 30,
                                   shared: true,
                                   restrictedToMinimumLevel: minimumLevel))))

                // ── Database ──────────────────────────────────────────────────
                //    HTTP request events ONLY — no EF noise in the DB table.
                .WriteTo.Logger(sub =>
                    sub.Filter.ByIncludingOnly(IsSerilogRequestLogging)
                       .WriteTo.PostgreSQL(
                           connectionString,
                           tableName,
                           columnWriters,
                           needAutoCreateTable: false));
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  App-phase: middleware extensions
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Pushes per-request properties (UserId, path, method, query-string, date)
    /// into the Serilog LogContext so every downstream log event carries them.
    /// </summary>
    public static void UseRequestLogEnrichment(this WebApplication app)
    {
        app.Use(async (HttpContext context, RequestDelegate next) =>
        {
            using (LogContext.PushProperty("LogDate", DateTime.UtcNow.ToString("ddMMyyyy")))
            using (LogContext.PushProperty("UserId", ResolveUserId(context)))
            using (LogContext.PushProperty("RequestPath", context.Request.Path.Value ?? string.Empty))
            using (LogContext.PushProperty("HttpMethod", context.Request.Method))
            using (LogContext.PushProperty("QueryString", context.Request.QueryString.Value ?? string.Empty))
            {
                await next(context);
            }
        });
    }

    /// <summary>
    /// Buffers request and response bodies so they can be forwarded to
    /// <see cref="Serilog.AspNetCore.RequestLoggingOptions.EnrichDiagnosticContext"/>.
    /// </summary>
    public static void UseRequestResponseBodyLogging(this WebApplication app)
    {
        app.Use(async (HttpContext context, RequestDelegate next) =>
        {
            var requestBody = await ReadRequestBodyAsync(context.Request);
            context.Items["RequestBody"] = requestBody;

            var originalBody = context.Response.Body;
            await using var responseBodyBuffer = new MemoryStream();
            context.Response.Body = responseBodyBuffer;

            await next(context);

            var responseText = await ReadResponseBodyAsync(context.Response);
            context.Response.Body = originalBody;
            await responseBodyBuffer.CopyToAsync(originalBody);

            context.Items["ResponseBody"] = responseText;
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static LogEventLevel ParseLogLevel(string? value) =>
        Enum.TryParse<LogEventLevel>(value, true, out var level) ? level : LogEventLevel.Information;

    private static string ResolveUserId(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? context.User.FindFirstValue("sub")
                     ?? "anonymous";

        return SanitizePathSegment(userId);
    }

    private static string SanitizePathSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "unknown";
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray()).Trim();
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is null or 0)
            return string.Empty;

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
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;

        if (contentType is null ||
            !contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return body;

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
        if (string.IsNullOrWhiteSpace(tableName)) return "\"ApplicationLogs\"";
        if (tableName.StartsWith("\"") && tableName.EndsWith("\"")) return tableName;
        return tableName.Any(char.IsUpper) ? $"\"{tableName}\"" : tableName;
    }

    private static bool IsSerilogRequestLogging(LogEvent logEvent)
    {
        return logEvent.Properties.TryGetValue("SourceContext", out var propertyValue) &&
               propertyValue is ScalarValue { Value: "Serilog.AspNetCore.RequestLoggingMiddleware" };
    }

    private static bool IsEfCommand(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("SourceContext", out var ctx)) return false;
        return ctx is ScalarValue { Value: string src } &&
               src.Contains("Microsoft.EntityFrameworkCore.Database.Command", StringComparison.Ordinal);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Custom Serilog formatter / column writer
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Writes each log event as a compact human-readable block in the file sink:
    /// <code>
    /// 2026-05-03 17:53:07 +07:00 [INF] HTTP POST /api/orders responded 200 in 18.7ms
    ///   QueryString : ?page=1
    ///   RequestBody : { "name": "Pho" }
    ///   ResponseBody: { "id": 42 }
    /// </code>
    /// For EF Core events the SQL appears directly in the rendered message.
    /// Empty / null context lines are suppressed.
    /// </summary>
    private sealed class RequestResponseTextFormatter : ITextFormatter
    {
        public void Format(LogEvent logEvent, TextWriter output)
        {
            // Header line
            output.Write("{0:yyyy-MM-dd HH:mm:ss zzz} [{1}] ",
                logEvent.Timestamp,
                logEvent.Level.ToString()[..3].ToUpperInvariant());
            logEvent.RenderMessage(output);
            output.WriteLine();

            // Exception (if any)
            if (logEvent.Exception is not null)
                output.WriteLine("  Exception   : {0}", logEvent.Exception);

            // Context lines — HTTP events only; EF events have no body/query.
            WriteIfPresent(output, logEvent, "QueryString", "QueryString ");
            WriteIfPresent(output, logEvent, "RequestBody", "RequestBody ");
            WriteIfPresent(output, logEvent, "ResponseBody", "ResponseBody");

            output.WriteLine();
        }

        private static void WriteIfPresent(
            TextWriter output, LogEvent logEvent, string propertyName, string label)
        {
            if (!logEvent.Properties.TryGetValue(propertyName, out var prop)) return;
            var value = prop is ScalarValue sv ? sv.Value?.ToString() : prop.ToString();
            if (string.IsNullOrWhiteSpace(value)) return;
            output.WriteLine("  {0}: {1}", label, value);
        }
    }

    /// <summary>
    /// Serialises HTTP-request context into the JSONB <c>Properties</c> column.
    /// Dedicated DB columns (RequestPath, HttpMethod, UserId) are excluded to
    /// avoid duplication; internal Serilog/ASP.NET noise is also stripped.
    /// </summary>
    private sealed class HttpRequestPropertiesColumnWriter : ColumnWriterBase
    {
        private static readonly HashSet<string> ExcludedFromJsonb =
        [
            "RequestPath", "HttpMethod", "UserId",
            "SourceContext", "RequestId", "ConnectionId",
            "ActionId", "ActionName", "TraceId", "SpanId",
            "LogDate"
        ];

        public HttpRequestPropertiesColumnWriter() : base(NpgsqlDbType.Jsonb) { }

        public override object GetValue(LogEvent logEvent, IFormatProvider formatProvider)
        {
            var payload = new Dictionary<string, object?>
            {
                ["RequestBody"] = ReadPropertyValue(logEvent, "RequestBody"),
                ["ResponseBody"] = ReadPropertyValue(logEvent, "ResponseBody"),
                ["QueryString"] = ReadPropertyValue(logEvent, "QueryString"),
                ["StatusCode"] = ReadPropertyValue(logEvent, "StatusCode"),
                ["Elapsed"] = ReadPropertyValue(logEvent, "Elapsed")
            };

            // Carry through any additional enrichment not already in a dedicated column
            foreach (var (key, value) in logEvent.Properties)
            {
                if (!ExcludedFromJsonb.Contains(key) && !payload.ContainsKey(key))
                    payload[key] = value is ScalarValue sv ? sv.Value : value.ToString();
            }

            return JsonSerializer.Serialize(payload);
        }

        private static object? ReadPropertyValue(LogEvent logEvent, string name) =>
            logEvent.Properties.TryGetValue(name, out var value)
                ? value is ScalarValue scalar ? scalar.Value : value.ToString()
                : null;
    }
}
