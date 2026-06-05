using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SettingAggregateRoot = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Application.MediatR.Setting.GetSettingById;

internal class GetSettingByIdQueryHandler(
    IGenericRepository<SettingAggregateRoot, Guid> settingRepository,
    ILogger<GetSettingByIdQueryHandler> logger
) : IQueryHandler<GetSettingByIdQuery, GetSettingByIdResponse>
{
    public async Task<Result<GetSettingByIdResponse>> Handle(
        GetSettingByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var setting = await settingRepository.GetByIdAsync(request.Id, cancellationToken);

            if (setting is null || setting.IsDeleted)
            {
                return Result.Failure<GetSettingByIdResponse>(
                    Error.NullValue,
                    "Setting not found.");
            }

            var response = new GetSettingByIdResponse
            {
                Id = setting.Id,
                Code = setting.Code,
                Name = setting.Name,
                Description = setting.Description,
                Group = setting.Group,
                Value = setting.Value,
                Type = setting.Type
            };

            return Result.Success(response, "Setting retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving setting by id {SettingId}", request.Id);
            return Result.Failure<GetSettingByIdResponse>(
                Error.ServerError,
                "An error occurred while retrieving setting.");
        }
    }
}

