using System.Diagnostics;
using SC.Infrastructure.Services.ApiLog;
using SC.Domain.Domain.Logging.AggregateRoot;
using SC.Domain.SharedKernel.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SC.Api.Middleware;

public class ApiLoggerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiLoggerMiddleware> _logger;

    public ApiLoggerMiddleware(RequestDelegate next, ILogger<ApiLoggerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiLogService apiLogService)
    {
        // Only log requests whose path contains "/api/".
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var startTime = DateTimeOffset.UtcNow;
        var sw = Stopwatch.StartNew();

        var requestBody = await ReadRequestBody(context.Request);

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        Exception? error = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            sw.Stop();

            // This finally block must never throw: an exception raised here would
            // replace the real pipeline exception and leave the response broken.
            var responseBodyText = await SafeReadResponseBody(context.Response);

            try
            {
                if (responseBody.CanSeek)
                {
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
            catch (Exception copyEx)
            {
                _logger.LogWarning(copyEx,
                    "ApiLoggerMiddleware: failed to copy the buffered response to the client stream.");
            }
            finally
            {
                // Restore the real stream so later middleware (e.g. the global
                // exception handler) can still write a response after this exits.
                context.Response.Body = originalBodyStream;
            }

            // Do not log OPTIONS preflight requests.
            if (context.Request.Method != "OPTIONS")
            {
                try
                {
                    var logLevel = error != null || context.Response.StatusCode >= 500
                        ? AppLogLevel.ERROR
                        : AppLogLevel.INFO;

                    var logItem = new ApiLog
                    {
                        ApiUrl = context.Request.Path + context.Request.QueryString,
                        ApiMethod = context.Request.Method,
                        ApiBody = requestBody,
                        ApiResponse = responseBodyText,
                        ErrorTrace = error?.ToString(),
                        Message = error?.Message ?? $"Responded {context.Response.StatusCode} in {sw.ElapsedMilliseconds}ms",
                        StatusCode = context.Response.StatusCode,
                        LocalIpAddress = context.Connection.RemoteIpAddress?.ToString(),
                        CreatedDate = startTime,
                        EndDate = DateTimeOffset.UtcNow,
                        RequestId = context.TraceIdentifier
                    };

                    await apiLogService.WriteLogAsync(logItem, logLevel);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "ApiLoggerMiddleware: failed to persist the API log entry.");
                }
            }
        }
    }

    private static async Task<string> ReadRequestBody(HttpRequest request)
    {
        if (IsBinaryContent(request.ContentType))
        {
            return $"[binary content - {request.ContentLength ?? 0} bytes - {request.ContentType}]";
        }

        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return Sanitize(body);
    }

    private static async Task<string> SafeReadResponseBody(HttpResponse response)
    {
        try
        {
            return await ReadResponseBody(response);
        }
        catch (Exception ex)
        {
            return $"[response body unavailable: {ex.GetType().Name}]";
        }
    }

    private static async Task<string> ReadResponseBody(HttpResponse response)
    {
        if (IsBinaryContent(response.ContentType))
        {
            return $"[binary content - {response.ContentLength ?? 0} bytes - {response.ContentType}]";
        }

        var body = response.Body;

        // A disposed stream reports false for all three capabilities — guard against
        // it so a closed response stream does not throw ObjectDisposedException.
        if (body is null || !body.CanRead || !body.CanSeek)
        {
            return "[response body not capturable]";
        }

        body.Seek(0, SeekOrigin.Begin);
        var text = await new StreamReader(body, leaveOpen: true).ReadToEndAsync();
        body.Seek(0, SeekOrigin.Begin);
        return Sanitize(text);
    }

    private static bool IsBinaryContent(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return false;
        var ct = contentType.ToLowerInvariant();
        return ct.StartsWith("multipart/")
            || ct.StartsWith("image/")
            || ct.StartsWith("video/")
            || ct.StartsWith("audio/")
            || ct.StartsWith("application/octet-stream")
            || ct.StartsWith("application/pdf")
            || ct.StartsWith("application/zip");
    }

    private static string Sanitize(string body)
    {
        // PostgreSQL text columns reject the null byte; strip just in case.
        return string.IsNullOrEmpty(body) ? body : body.Replace("\0", string.Empty);
    }
}
