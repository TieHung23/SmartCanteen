namespace SC.Domain.Domain.Logging.AggregateRoot;

public class ApiLog
{
    public Guid Id { get; set; }
    public string? LoginId { get; set; }
    public string LogLevel { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiMethod { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? ErrorTrace { get; set; }
    public string? ApiBody { get; set; }
    public string? ApiResponse { get; set; }
    public string? LocalIpAddress { get; set; }
    public string? LocalHostPC { get; set; }
    public string? LogApp { get; set; }
    public string? LogVersion { get; set; }
    public string? Memo { get; set; }
    public string? RequestId { get; set; }
    public string? ApiDesc { get; set; }
    public string? ApiVer { get; set; }
    public int StatusCode { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
}
