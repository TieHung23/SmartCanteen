using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Logging.Admin.GetLogDetail;

public record GetApiLogDetailQuery(Guid Id) : IQuery<LogDetailResponse>;

public record LogDetailResponse(
    Guid Id,
    string? LoginId,
    string LogLevel,
    string ApiUrl,
    string ApiMethod,
    string? ApiBody,
    string? ApiResponse,
    string? Message,
    int StatusCode,
    string? ErrorTrace,
    string? LocalIpAddress,
    string? RequestId,
    DateTimeOffset CreatedDate,
    DateTimeOffset? EndDate);
