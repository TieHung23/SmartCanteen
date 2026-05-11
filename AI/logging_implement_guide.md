# API Logging Implementation Guide
> **Stack:** ASP.NET Core 8 · Entity Framework Core · PostgreSQL (Npgsql) · Serilog

Two logging mechanisms:
1. **DB Logging** — every HTTP request/response is written to `fosv3_apilog` table via EF Core
2. **File Logging** — diagnostic text logs routed to per-user rolling files via Serilog

---

## 1. NuGet Packages

```xml
<!-- EF Core + PostgreSQL provider -->
<PackageReference Include="Microsoft.EntityFrameworkCore"           Version="8.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design"    Version="8.*" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL"   Version="8.*" />

<!-- Serilog file logging -->
<PackageReference Include="Serilog"                          Version="4.0.0" />
<PackageReference Include="Serilog.Sinks.File"              Version="5.0.0" />
<PackageReference Include="Serilog.Sinks.Map"               Version="2.0.0" />
<PackageReference Include="Serilog.Extensions.Hosting"      Version="8.0.0" />
```

---

## 2. PostgreSQL Table (auto-created by EF Migrations)

EF Core creates this table automatically. For reference, the equivalent DDL is:

```sql
CREATE TABLE application_apilog (
    id               BIGSERIAL    PRIMARY KEY,
    login_id         VARCHAR(100),
    log_level        VARCHAR(10)  NOT NULL,
    api_url          TEXT         NOT NULL,
    api_method       VARCHAR(10)  NOT NULL,
    message          TEXT,
    error_trace      TEXT,
    api_body         TEXT,
    api_response     TEXT,
    local_ip_address VARCHAR(50),
    local_host_pc    VARCHAR(200),
    log_app          VARCHAR(200),
    log_version      VARCHAR(50),
    memo             TEXT,
    request_id       VARCHAR(200),
    api_desc         TEXT,
    api_ver          VARCHAR(50),
    created_date     TIMESTAMP    NOT NULL,
    end_date         TIMESTAMP
);

CREATE INDEX ix_apilog_created_date ON application_apilog (created_date);
CREATE INDEX ix_apilog_request_id   ON application_apilog (request_id);
```

---

## 3. Log Level Enum

```csharp
// Common/Enums/AppLogLevel.cs
namespace YourProject.Common.Enums
{
    public enum AppLogLevel
    {
        ERROR = 0,
        INFO  = 1,
        DEBUG = 2,
    }
}
```

---

## 4. ApiLog Entity

```csharp
// Data/Entities/ApiLog.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YourProject.Data.Entities
{
    [Table("application_apilog")]
    public class ApiLog
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("login_id")]    [MaxLength(100)]  public string LoginId        { get; set; }
        [Column("log_level")]   [MaxLength(10)]   [Required] public string LogLevel { get; set; }
        [Column("api_url")]     [Required]        public string ApiUrl         { get; set; }
        [Column("api_method")]  [MaxLength(10)]   [Required] public string ApiMethod { get; set; }
        [Column("message")]                       public string Message        { get; set; }
        [Column("error_trace")]                   public string ErrorTrace     { get; set; }
        [Column("api_body")]                      public string ApiBody        { get; set; }
        [Column("api_response")]                  public string ApiResponse    { get; set; }
        [Column("local_ip_address")] [MaxLength(50)]  public string LocalIpAddress { get; set; }
        [Column("local_host_pc")]    [MaxLength(200)] public string LocalHostPC    { get; set; }
        [Column("log_app")]          [MaxLength(200)] public string LogApp         { get; set; }
        [Column("log_version")]      [MaxLength(50)]  public string LogVersion     { get; set; }
        [Column("memo")]                          public string Memo           { get; set; }
        [Column("request_id")]  [MaxLength(200)]  public string RequestId      { get; set; }
        [Column("api_desc")]                      public string ApiDesc        { get; set; }
        [Column("api_ver")]     [MaxLength(50)]   public string ApiVer         { get; set; }
        [Column("created_date")] [Required]       public DateTime CreatedDate  { get; set; }
        [Column("end_date")]                      public DateTime? EndDate     { get; set; }
    }
}
```

---

## 5. DbContext

```csharp
// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using YourProject.Data.Entities;

namespace YourProject.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<ApiLog> ApiLogs => Set<ApiLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApiLog>(e =>
            {
                e.ToTable("application_apilog");
                e.HasIndex(x => x.CreatedDate).HasDatabaseName("ix_apilog_created_date");
                e.HasIndex(x => x.RequestId).HasDatabaseName("ix_apilog_request_id");

                // PostgreSQL: use lowercase column names
                foreach (var prop in e.Metadata.GetProperties())
                    prop.SetColumnName(prop.Name.ToSnakeCase());
            });
        }
    }
}
```

> **Best practice:** EF Core with Npgsql uses snake_case column names by default if you call `UseSnakeCaseNamingConvention()` (see Section 12). The `Column` attributes above are an explicit alternative.

---

## 6. Snake Case Naming Helper (optional but recommended)

If you use `UseSnakeCaseNamingConvention()` from `EFCore.NamingConventions`, you can remove all `[Column]` attributes:

```xml
<PackageReference Include="EFCore.NamingConventions" Version="8.*" />
```

```csharp
// In DI registration (Section 12):
options.UseNpgsql(connectionString)
       .UseSnakeCaseNamingConvention();  // auto maps LoginId → login_id
```

---

## 7. Repository Interface

```csharp
// Repository/Interfaces/IApiLogRepository.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using YourProject.Common.Enums;
using YourProject.Data.Entities;

namespace YourProject.Repository.Interfaces
{
    public interface IApiLogRepository
    {
        Task SaveAsync(ApiLog apiLog, AppLogLevel logLevel = AppLogLevel.DEBUG);
        Task<List<ApiLog>> GetOldLogsAsync(int daysOld = 60, int limit = 500);
        Task<int> CountOldLogsAsync(int daysOld = 60);
        Task<int> DeleteByRequestIdsAsync(IEnumerable<string> requestIds);
    }
}
```

---

## 8. Repository Implementation (EF Core Best Practices)

```csharp
// Repository/ApiLogRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YourProject.Common.Enums;
using YourProject.Data;
using YourProject.Data.Entities;
using YourProject.Repository.Interfaces;

namespace YourProject.Repository
{
    public class ApiLogRepository : IApiLogRepository
    {
        private readonly AppDbContext _db;

        public ApiLogRepository(AppDbContext db)
        {
            _db = db;
        }

        // ── SAVE ──────────────────────────────────────────────────────────────
        // Best practice: fire-and-forget via a NEW DbContext scope so it never
        // blocks the HTTP response and never conflicts with the request's DbContext.
        public async Task SaveAsync(ApiLog apiLog, AppLogLevel logLevel = AppLogLevel.DEBUG)
        {
            apiLog.LogLevel = ToStr(logLevel);

            // Sanitise backslash sequences in JSON text fields
            apiLog.ErrorTrace  = Escape(apiLog.ErrorTrace);
            apiLog.ApiBody     = Escape(apiLog.ApiBody);
            apiLog.ApiResponse = Escape(apiLog.ApiResponse);

            _db.ApiLogs.Add(apiLog);
            await _db.SaveChangesAsync();
        }

        // ── GET (for backup export) ────────────────────────────────────────────
        public async Task<List<ApiLog>> GetOldLogsAsync(int daysOld = 60, int limit = 500)
        {
            var cutoff = DateTime.UtcNow.AddDays(-daysOld);
            return await _db.ApiLogs
                .AsNoTracking()              // read-only: skip change tracking overhead
                .Where(x => x.CreatedDate < cutoff)
                .OrderBy(x => x.CreatedDate)
                .Take(limit)
                .ToListAsync();
        }

        // ── COUNT ──────────────────────────────────────────────────────────────
        public async Task<int> CountOldLogsAsync(int daysOld = 60)
        {
            var cutoff = DateTime.UtcNow.AddDays(-daysOld);
            return await _db.ApiLogs
                .AsNoTracking()
                .CountAsync(x => x.CreatedDate < cutoff);
        }

        // ── DELETE by request IDs ─────────────────────────────────────────────
        // EF Core 7+: ExecuteDeleteAsync avoids loading entities into memory
        public async Task<int> DeleteByRequestIdsAsync(IEnumerable<string> requestIds)
        {
            var ids = requestIds.ToList();
            if (!ids.Any()) return 0;

            return await _db.ApiLogs
                .Where(x => ids.Contains(x.RequestId))
                .ExecuteDeleteAsync();        // EF Core 7+ bulk delete — no tracking
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static string ToStr(AppLogLevel l) => l switch
        {
            AppLogLevel.INFO  => "INFO",
            AppLogLevel.ERROR => "ERROR",
            _                 => "DEBUG",
        };

        private static string Escape(string s) =>
            string.IsNullOrEmpty(s) ? string.Empty : s.Replace("\\", "\\\\");
    }
}
```

---

## 9. Service Layer

```csharp
// Services/Interfaces/IApiLogService.cs
using System.Threading.Tasks;
using YourProject.Data.Entities;

namespace YourProject.Services.Interfaces
{
    public interface IApiLogService
    {
        Task DebugAsync(ApiLog log);
        Task InfoAsync(ApiLog log);
        Task ErrorAsync(ApiLog log);
    }
}
```

```csharp
// Services/ApiLogService.cs
using System.Threading.Tasks;
using YourProject.Common.Enums;
using YourProject.Data.Entities;
using YourProject.Repository.Interfaces;
using YourProject.Services.Interfaces;

namespace YourProject.Services
{
    public class ApiLogService : IApiLogService
    {
        private readonly IApiLogRepository _repo;

        public ApiLogService(IApiLogRepository repo) => _repo = repo;

        public Task DebugAsync(ApiLog log) => _repo.SaveAsync(log, AppLogLevel.DEBUG);
        public Task InfoAsync(ApiLog log)  => _repo.SaveAsync(log, AppLogLevel.INFO);
        public Task ErrorAsync(ApiLog log) => _repo.SaveAsync(log, AppLogLevel.ERROR);
    }
}
```

---

## 10. Middleware — Capture Every Request & Response

```csharp
// Middlewares/ApiLoggerMiddleware.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using YourProject.Data.Entities;
using YourProject.Services.Interfaces;

namespace YourProject.Middlewares
{
    public class ApiLoggerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public ApiLoggerMiddleware(RequestDelegate next)
        {
            _next   = next;
            _logger = Log.Logger;
        }

        public async Task Invoke(HttpContext context)
        {
            // IMPORTANT: resolve IApiLogService from a NEW scope so the DbContext
            // used for logging is isolated from the request's own DbContext.
            using var scope = context.RequestServices.CreateScope();
            var logService  = scope.ServiceProvider.GetRequiredService<IApiLogService>();

            var originalBody = context.Response.Body;
            using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            var log = new ApiLog
            {
                CreatedDate    = DateTime.UtcNow,
                ApiMethod      = context.Request.Method,
                ApiUrl         = context.Request.GetDisplayUrl(),
                RequestId      = context.TraceIdentifier,
                ApiVer         = "v1",   // replace with your version constant
                LocalIpAddress = context.Connection.RemoteIpAddress?.ToString(),
            };

            // Pull authenticated user (set by auth/JWT middleware earlier)
            // var user = context.Items["User"] as UserAuth;
            // if (user != null) { log.LoginId = user.UserID; ... }

            try
            {
                // Capture request body
                context.Request.EnableBuffering();
                log.ApiBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;

                await _next(context);

                // Capture response body
                buffer.Seek(0, SeekOrigin.Begin);
                log.ApiResponse = await new StreamReader(buffer).ReadToEndAsync();
                buffer.Seek(0, SeekOrigin.Begin);
                await buffer.CopyToAsync(originalBody);

                log.Message = "API SUCCESS";
                log.EndDate = DateTime.UtcNow;

                _logger.Information("Log DB start [{Url}] id:{Id}", log.ApiUrl, log.RequestId);
                await logService.InfoAsync(log);
            }
            catch (Exception ex)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode  = 200;

                var errJson = System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
                await context.Response.WriteAsync(errJson);

                buffer.Seek(0, SeekOrigin.Begin);
                log.ApiResponse = await new StreamReader(buffer).ReadToEndAsync();
                buffer.Seek(0, SeekOrigin.Begin);
                await buffer.CopyToAsync(originalBody);

                log.Message    = "API ERROR";
                log.ErrorTrace = FlattenException(ex);
                log.EndDate    = DateTime.UtcNow;

                _logger.Error(ex, "API error [{Url}]", log.ApiUrl);
                await logService.ErrorAsync(log);
            }
        }

        private static string FlattenException(Exception ex)
        {
            if (ex == null) return string.Empty;
            return $"{ex.Message}\n{ex.StackTrace}" +
                   (ex.InnerException != null ? "\n" + FlattenException(ex.InnerException) : "");
        }
    }
}
```

> **Why `CreateScope()` inside middleware?**  
> `AppDbContext` is registered as `Scoped`. Middleware is a singleton, so it cannot inject scoped services directly.  
> Creating a child scope gives us a fresh `DbContext` isolated from the controller's context — avoiding concurrency exceptions.

---

## 11. File Logging — Serilog Setup

### `addOnConfig.json`

```json
{
  "Serilog": {
    "MinimumLevel": "Debug",
    "FilePath":     "%SystemDrive%/Logs/YourApp/%ASPNETCORE_ENVIRONMENT%/",
    "RollingInterval":    "Day",
    "FileSizeLimitBytes": 100000000
  }
}
```

### Custom enricher — routes log files per user per day

```csharp
// Logging/DynamicUserIdEnricher.cs
using Serilog.Core;
using Serilog.Events;
using System;

namespace YourProject.Logging
{
    // Adds "UserId-Date" = "{userId}{yyMMdd}" property to every log event.
    // Serilog.Sinks.Map uses this to route each user's logs to a separate file.
    public class DynamicUserIdEnricher : ILogEventEnricher
    {
        private const string DateFmt = "yyMMdd";

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
        {
            var key = logEvent.Properties.TryGetValue("UserId", out var prop)
                ? $"{prop.ToString().Trim('"')}{DateTime.Now.ToString(DateFmt)}"
                : DateTime.Now.ToString(DateFmt);

            logEvent.AddPropertyIfAbsent(factory.CreateProperty("UserId-Date", key));
        }
    }
}
```

### `Program.cs` — configure Serilog before `builder.Build()`

```csharp
using Serilog;
using Serilog.Events;
using YourProject.Logging;

var serilogCfg = new ConfigurationBuilder()
    .AddJsonFile("addOnConfig.json", optional: true, reloadOnChange: true)
    .Build().GetSection("Serilog");

var minLevel      = serilogCfg.GetValue<string>("MinimumLevel");       // "Debug"
var rolling       = serilogCfg.GetValue<string>("RollingInterval");    // "Day"
var rawPath       = serilogCfg.GetValue<string>("FilePath");
var fileLimit     = serilogCfg.GetValue<int>("FileSizeLimitBytes");

var basePath = Environment.ExpandEnvironmentVariables(rawPath);
const int dateSuffixLen = 6; // length of "yyMMdd"

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.With(new DynamicUserIdEnricher())
    .WriteTo.Logger(lc => lc
        .WriteTo.Map("UserId-Date", string.Empty, (name, wt) =>
        {
            var userId  = name.Length > dateSuffixLen ? name[..^dateSuffixLen] : string.Empty;
            var logPath = $"{basePath}/{DateTime.Now:yyyy/MM/dd}/{userId}/.log";

            wt.File(logPath,
                restrictedToMinimumLevel: Enum.Parse<LogEventLevel>(minLevel),
                fileSizeLimitBytes:       fileLimit,
                rollOnFileSizeLimit:      true,
                rollingInterval:          Enum.Parse<RollingInterval>(rolling),
                retainedFileCountLimit:   null);
        }))
    .CreateLogger();

builder.Host.UseSerilog(); // wire Serilog into ASP.NET Core's ILogger
```

### Push UserId into context (in auth middleware)

```csharp
// In your JWT/auth middleware, after token validation:
using Serilog.Context;

LogContext.PushProperty("UserId", userId ?? string.Empty);
// Every _logger call in this request now routes to that user's file.
```

---

## 12. Full DI Registration

```csharp
// Program.cs
using Microsoft.EntityFrameworkCore;
using YourProject.Data;
using YourProject.Middlewares;
using YourProject.Repository;
using YourProject.Repository.Interfaces;
using YourProject.Services;
using YourProject.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ── EF Core — PostgreSQL ──────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseSnakeCaseNamingConvention()   // requires EFCore.NamingConventions package
           .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
           .EnableDetailedErrors(builder.Environment.IsDevelopment()));

// ── API Log Repository & Service ──────────────────────────────────────────
builder.Services.AddScoped<IApiLogRepository, ApiLogRepository>();
builder.Services.AddScoped<IApiLogService,    ApiLogService>();

builder.Services.AddControllers();
// ... add other services

var app = builder.Build();

// ── Apply EF Migrations on startup (dev/staging) ──────────────────────────
// Remove in production — run `dotnet ef database update` in CI/CD instead
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// ── Middleware order ───────────────────────────────────────────────────────
app.UseMiddleware<ApiLoggerMiddleware>();   // must be early in pipeline
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=yourdb;Username=user;Password=pass;"
  }
}
```

---

## 13. EF Core Migrations

```bash
# Add initial migration
dotnet ef migrations add InitApiLog --project YourProject

# Apply to database
dotnet ef database update

# (Optional) Generate SQL script for review before applying
dotnet ef migrations script --output migration.sql
```

---

## 14. Using File Logger in Services

```csharp
using Serilog;
using Microsoft.Extensions.Logging;

public class SomeService
{
    // Option A: Serilog static logger (same pattern as IkkankaApi)
    private readonly ILogger _serilog = Log.Logger;

    // Option B: ASP.NET Core ILogger<T> (preferred when using builder.Host.UseSerilog())
    private readonly Microsoft.Extensions.Logging.ILogger<SomeService> _logger;

    public SomeService(Microsoft.Extensions.Logging.ILogger<SomeService> logger)
    {
        _logger = logger;
    }

    public void DoWork()
    {
        _logger.LogInformation("Starting DoWork");
        try { /* ... */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DoWork failed: {Message}", ex.Message);
            throw;
        }
    }
}
```

---

## 15. EF Core Best Practices Summary

| Practice | Detail |
|---|---|
| `AsNoTracking()` | Always use on read-only queries — avoids change-tracker overhead |
| `ExecuteDeleteAsync()` | Bulk delete (EF Core 7+) without loading entities into memory |
| Isolated scope in middleware | Create `IServiceScope` inside middleware `Invoke()` to get a fresh `DbContext` |
| Async all the way | Use `SaveChangesAsync`, `ToListAsync`, `CountAsync` — never block |
| `MigrateAsync()` on startup | Auto-apply migrations in dev/staging; use CI script in production |
| `UseSnakeCaseNamingConvention()` | Maps `LoginId` → `login_id` automatically — no `[Column]` attributes needed |
| `EnableSensitiveDataLogging` | Enable only in Development — logs parameter values for debugging |
| No `SaveChanges` in loops | Batch inserts: add multiple entities then call `SaveChangesAsync()` once |

---

## 16. Summary — Two Systems Side-by-Side

| | DB Logging | File Logging |
|---|---|---|
| **Written by** | `ApiLoggerMiddleware` → `ApiLogService` | `_logger.Log*()` calls anywhere |
| **Storage** | PostgreSQL `fosv3_apilog` | Rolling `.log` files per user per day |
| **ORM / Library** | EF Core 8 + Npgsql | Serilog + Sinks.Map + Sinks.File |
| **Config** | `appsettings.json` (connection string) | `addOnConfig.json` (Serilog section) |
| **Per-user routing** | `login_id` column filter | `DynamicUserIdEnricher` + `WriteTo.Map` |
| **Cleanup** | `ExecuteDeleteAsync` in scheduled job | OS / file rotation |
