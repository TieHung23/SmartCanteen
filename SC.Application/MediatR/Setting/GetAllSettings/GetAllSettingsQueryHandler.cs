using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SettingAggregateRoot = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Application.MediatR.Setting.GetAllSettings;

internal class GetAllSettingsQueryHandler(
    IRepositoryBase<SettingAggregateRoot, Guid> settingRepository,
    ILogger<GetAllSettingsQueryHandler> logger
) : IQueryHandler<GetAllSettingsQuery, PaginatedList<GetAllSettingsResponse>>
{
    public async Task<Result<PaginatedList<GetAllSettingsResponse>>> Handle(
        GetAllSettingsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            IQueryable<SettingAggregateRoot> query = settingRepository.FindAll(
                x => !x.IsDeleted,
                cancellationToken: cancellationToken)!;

            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                query = query.Where(x =>
                    x.Code.Contains(request.Code, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                query = query.Where(x =>
                    x.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(request.Group))
            {
                query = query.Where(x =>
                    x.Group.Contains(request.Group, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                query = query.Where(x =>
                    x.Type.Contains(request.Type, StringComparison.OrdinalIgnoreCase));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var skipCount = request.GetSkipCount();
            var paginatedSettings = await query
                .OrderBy(x => x.Group)
                .ThenBy(x => x.Code)
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var responses = paginatedSettings.Select(s => new GetAllSettingsResponse
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                Group = s.Group,
                Value = s.Value,
                Type = s.Type
            }).ToList();

            var paginatedResult = new PaginatedList<GetAllSettingsResponse>(
                responses,
                request.PageNumber,
                request.PageSize,
                totalCount);

            return Result.Success(paginatedResult, "Settings retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving settings");
            return Result.Failure<PaginatedList<GetAllSettingsResponse>>(
                Error.ServerError,
                "An error occurred while retrieving settings.");
        }
    }
}
