using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;

namespace SC.Application.MediatR.Logging.Admin.ListLogs;

internal sealed class ListApiLogsQueryHandler(
    IApiLogRepository apiLogRepository,
    ILogger<ListApiLogsQueryHandler> logger)
    : IQueryHandler<ListApiLogsQuery, PaginatedList<LogListItem>>
{
    public async Task<Result<PaginatedList<LogListItem>>> Handle(
        ListApiLogsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var allLogs = await apiLogRepository.GetFilteredLogsAsync(
                request.LogLevel,
                request.Method,
                request.Url,
                request.StatusCodeMin,
                request.FromDate,
                request.ToDate,
                cancellationToken);

            var totalCount = allLogs.Count;
            var page = allLogs
                .Skip(request.GetSkipCount())
                .Take(request.PageSize)
                .ToList();

            var items = page.Select(x => new LogListItem(
                x.Id,
                x.LogLevel,
                x.ApiUrl,
                x.ApiMethod,
                x.Message,
                x.StatusCode,
                x.LocalIpAddress,
                x.RequestId,
                x.CreatedDate,
                x.EndDate)).ToList();

            return Result.Success(
                new PaginatedList<LogListItem>(items, request.PageNumber, request.PageSize, totalCount),
                "API logs retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing API logs");
            return Result.Failure<PaginatedList<LogListItem>>(
                Error.ServerError,
                "An error occurred while retrieving API logs.");
        }
    }
}
