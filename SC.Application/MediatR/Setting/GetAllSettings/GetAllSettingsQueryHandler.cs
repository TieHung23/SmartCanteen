using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SettingAggregateRoot = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Application.MediatR.Setting.GetAllSettings;

internal class GetAllSettingsQueryHandler(
    IGenericRepository<SettingAggregateRoot, Guid> settingRepository,
    ILogger<GetAllSettingsQueryHandler> logger
) : IQueryHandler<GetAllSettingsQuery, PaginatedList<GetAllSettingsResponse>>
{
    public async Task<Result<PaginatedList<GetAllSettingsResponse>>> Handle(
        GetAllSettingsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var allSettings = await settingRepository
                .FindListAsync(x => !x.IsDeleted, cancellationToken);
            var filtered = allSettings.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var code = request.Code.Trim().ToLower();
                filtered = filtered.Where(x =>
                    x.Code.ToLower().Contains(code));
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var name = request.Name.Trim().ToLower();
                filtered = filtered.Where(x =>
                    x.Name.ToLower().Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(request.Group))
            {
                var group = request.Group.Trim().ToLower();
                filtered = filtered.Where(x =>
                    x.Group.ToLower().Contains(group));
            }

            if (!string.IsNullOrWhiteSpace(request.Scope))
            {
                var scope = request.Scope.Trim().ToLower();
                filtered = filtered.Where(x =>
                    x.Scope.ToLower().Contains(scope));
            }

            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                var type = request.Type.Trim().ToLower();
                filtered = filtered.Where(x =>
                    x.Type.ToLower().Contains(type));
            }

            var filteredList = filtered.ToList();
            var totalCount = filteredList.Count;

            var skipCount = request.GetSkipCount();
            var paginatedSettings = filteredList
                .OrderBy(x => x.Group)
                .ThenBy(x => x.Scope)
                .ThenBy(x => x.Code)
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToList();

            var responses = paginatedSettings.Select(s => new GetAllSettingsResponse
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                Group = s.Group,
                Scope = s.Scope,
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
