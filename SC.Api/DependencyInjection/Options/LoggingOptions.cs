namespace SC.Api.DependencyInjection.Options;

public sealed class LoggingOptions
{
    public const string SectionName = "AppLogging";

    public string MinimumLevel { get; set; } = "Information";

    public LoggingDatabaseOptions Database { get; set; } = new();

    /// <summary>
    /// Per-namespace minimum level overrides.
    /// Example: { "Microsoft": "Warning", "Microsoft.EntityFrameworkCore.Database.Command": "Information" }
    /// </summary>
    public Dictionary<string, string> Overrides { get; set; } = new();
}

public sealed class LoggingDatabaseOptions
{
    public string TableName { get; set; } = "\"ApplicationLogs\"";
}
