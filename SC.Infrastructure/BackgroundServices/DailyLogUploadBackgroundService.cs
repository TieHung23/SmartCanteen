using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using SC.Infrastructure.DependencyInjection.Options;
using SC.Infrastructure.Services.Cloudinary;

namespace SC.Infrastructure.BackgroundServices;

public sealed class DailyLogUploadBackgroundService(
    IOptions<CloundinaryOptions> cloundinaryOptions,
    IOptions<LogUploadOptions> logUploadOptions,
    ILogger<DailyLogUploadBackgroundService> logger,
    IHostEnvironment hostEnvironment,
    IServiceScopeFactory scopeFactory)
    : BackgroundService
{
    private readonly CloundinaryOptions _cloundinaryOptions = cloundinaryOptions.Value;
    private readonly LogUploadOptions _logUploadOptions = logUploadOptions.Value;
    private readonly ILogger<DailyLogUploadBackgroundService> _logger = logger;
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyLogUploadBackgroundService is starting.");
        _logger.LogInformation("Background service will upload logs at {UploadTimeUtc} UTC.", _logUploadOptions.UploadTimeUtc);
        _logger.LogInformation("Time now is {CurrentTimeUtc} UTC.", DateTime.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextUploadUtc(_logUploadOptions.UploadTimeUtc);
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            await UploadPreviousDayLogs(stoppingToken);
        }
    }

    private async Task UploadPreviousDayLogs(CancellationToken stoppingToken)
    {
        if (!_logUploadOptions.Enabled)
        {
            _logger.LogInformation("Daily log upload is disabled.");
            return;
        }

        if (!IsCloudinaryConfigured())
        {
            _logger.LogWarning("Cloudinary configuration is missing. Log upload skipped.");
            return;
        }

        var logDate = DateTime.UtcNow.AddDays(-1).ToString("ddMMyyyy");
        var logsRoot = Path.Combine(_hostEnvironment.ContentRootPath, _logUploadOptions.LocalLogsPath);
        var dateFolder = Path.Combine(logsRoot, logDate);

        if (!Directory.Exists(dateFolder))
        {
            _logger.LogInformation("Log folder not found for {LogDate}: {Folder}", logDate, dateFolder);
            return;
        }

        var files = Directory.GetFiles(dateFolder, "*", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            _logger.LogInformation("No log files found for {LogDate}.", logDate);
            return;
        }

        _logger.LogInformation("Uploading {Count} log files for {LogDate}.", files.Length, logDate);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var cloundinaryUpload = scope.ServiceProvider.GetRequiredService<ICloundinaryUpload>();

        foreach (var file in files)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                var relativePath = Path.GetRelativePath(dateFolder, file).Replace('\\', '/');
                var relativeWithoutExtension = Path.ChangeExtension(relativePath, null) ?? relativePath;
                relativeWithoutExtension = relativeWithoutExtension.Replace('\\', '/');
                var publicId = $"{_cloundinaryOptions.LogsFolder}/{logDate}/{relativeWithoutExtension}";

                await using var stream = File.OpenRead(file);
                await cloundinaryUpload.UploadRawFileAsync(stream, Path.GetFileName(file), publicId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload log file {File}.", file);
            }
        }
    }

    private bool IsCloudinaryConfigured()
    {
        return !string.IsNullOrWhiteSpace(_cloundinaryOptions.CloudName)
               && !string.IsNullOrWhiteSpace(_cloundinaryOptions.ApiKey)
               && !string.IsNullOrWhiteSpace(_cloundinaryOptions.ApiSecret);
    }

    private static TimeSpan GetDelayUntilNextUploadUtc(TimeSpan uploadTimeUtc)
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.Add(uploadTimeUtc);
        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun - now;
    }
}
