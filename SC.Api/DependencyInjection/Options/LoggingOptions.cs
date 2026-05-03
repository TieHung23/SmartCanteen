namespace SC.Api.DependencyInjection.Options;

public sealed class LoggingOptions
{
    public const string SectionName = "Logging";

    public string MinimumLevel { get; set; } = "Information";

    public LoggingDatabaseOptions Database { get; set; } = new();
}

public sealed class LoggingDatabaseOptions
{
    public string TableName { get; set; } = "\"ApplicationLogs\"";
}
