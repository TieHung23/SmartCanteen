using SC.Api.DependencyInjection.Configurations;
Serilog.Debugging.SelfLog.Enable(Console.Error);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

// builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile(
    "appsettings.Notification.json",
    optional: true,
    reloadOnChange: true);

builder.Configuration
    .AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: true)
    .AddEnvironmentVariables();

builder.AddApiConfigurations();

var app = builder.Build();
app.UseApiConfigurations();

app.Run();
