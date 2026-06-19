using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Logging.Admin.ListLogs;

public class ListApiLogsQuery : PaginationParams, IQuery<PaginatedList<LogListItem>>
{
    public string? LogLevel { get; set; }
    public string? Method { get; set; }
    public string? Url { get; set; }
    public int? StatusCodeMin { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
}

public record LogListItem(
    Guid Id,
    string LogLevel,
    string ApiUrl,
    string ApiMethod,
    string? Message,
    int StatusCode,
    string? LocalIpAddress,
    string? RequestId,
    DateTimeOffset CreatedDate,
    DateTimeOffset? EndDate);
