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
            var responseBodyText = await ReadResponseBody(context.Response);

            // Copy response back to original stream
            await responseBody.CopyToAsync(originalBodyStream);

            // Do not log OPTIONS or common static files
            if (context.Request.Method != "OPTIONS")
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
                    LocalIpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    CreatedDate = startTime,
                    EndDate = DateTimeOffset.UtcNow,
                    RequestId = context.TraceIdentifier
                };

                // Optional: Fire-and-forget or wait
                await apiLogService.WriteLogAsync(logItem, logLevel);
            }
        }
    }

    private static async Task<string> ReadRequestBody(HttpRequest request)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private static async Task<string> ReadResponseBody(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var text = await new StreamReader(response.Body, leaveOpen: true).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);
        return text;
    }
}
