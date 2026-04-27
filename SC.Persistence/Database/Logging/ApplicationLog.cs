namespace SC.Persistence.Database.Logging;

public class ApplicationLog
{
    public long Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public DateTimeOffset TimeStamp { get; set; }
    public string? Exception { get; set; }
    public string? Properties { get; set; }
    public string? UserId { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
}