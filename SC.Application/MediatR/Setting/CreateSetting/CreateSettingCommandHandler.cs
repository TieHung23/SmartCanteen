using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SettingAggregateRoot = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Application.MediatR.Setting.CreateSetting;

internal class CreateSettingCommandHandler(
    IGenericRepository<SettingAggregateRoot, Guid> settingRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<CreateSettingCommandHandler> logger
) : ICommandHandler<CreateSettingCommand, CreateSettingResponse>
{
    public async Task<Result<CreateSettingResponse>> Handle(
        CreateSettingCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedCode = request.Code.Trim();
            var existing = await settingRepository.GetQueryable(
                x => !x.IsDeleted && x.Code.ToLower() == normalizedCode.ToLower())
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                return Result.Failure<CreateSettingResponse>(
                    Error.InvalidValue,
                    "Setting code already exists.");
            }

            var setting = SettingAggregateRoot.Create(
                normalizedCode,
                request.Name.Trim(),
                request.Description?.Trim() ?? string.Empty,
                request.Group.Trim(),
                request.Value.Trim(),
                request.Type.Trim(),
                currentUserService.UserId);

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await settingRepository.AddAsync(setting, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new CreateSettingResponse
            {
                Id = setting.Id,
                Code = setting.Code,
                Name = setting.Name,
                Description = setting.Description,
                Group = setting.Group,
                Value = setting.Value,
                Type = setting.Type
            };

            return Result.Success(response, "Setting created successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error creating setting");
            return Result.Failure<CreateSettingResponse>(
                Error.ServerError,
                "An error occurred while creating setting.");
        }
    }
}

