using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;

namespace SC.Application.MediatR.Logging.Admin.GetLogDetail;

internal sealed class GetApiLogDetailQueryHandler(
    IApiLogRepository apiLogRepository,
    ILogger<GetApiLogDetailQueryHandler> logger)
    : IQueryHandler<GetApiLogDetailQuery, LogDetailResponse>
{
    public async Task<Result<LogDetailResponse>> Handle(
        GetApiLogDetailQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = await apiLogRepository.GetByIdAsync(request.Id, cancellationToken);

            if (log is null)
                return Result.Failure<LogDetailResponse>(
                    new Error("ApiLogNotFound", "API log not found.", 404),
                    "API log not found.");

            var response = new LogDetailResponse(
                log.Id,
                log.LoginId,
                log.LogLevel,
                log.ApiUrl,
                log.ApiMethod,
                log.ApiBody,
                log.ApiResponse,
                log.Message,
                log.StatusCode,
                log.ErrorTrace,
                log.LocalIpAddress,
                log.RequestId,
                log.CreatedDate,
                log.EndDate);

            return Result.Success(response, "API log detail retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving API log detail {Id}", request.Id);
            return Result.Failure<LogDetailResponse>(
                Error.ServerError,
                "An error occurred while retrieving the API log detail.");
        }
    }
}
