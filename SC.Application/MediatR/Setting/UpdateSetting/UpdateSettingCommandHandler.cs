using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Application.MediatR.RefundPolicy;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SettingAggregateRoot = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Application.MediatR.Setting.UpdateSetting;

internal class UpdateSettingCommandHandler(
    IGenericRepository<SettingAggregateRoot, Guid> settingRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<UpdateSettingCommandHandler> logger
) : ICommandHandler<UpdateSettingCommand, UpdateSettingResponse>
{
    public async Task<Result<UpdateSettingResponse>> Handle(
        UpdateSettingCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var setting = await settingRepository.GetByIdAsync(request.Id, cancellationToken);
            if (setting is null || setting.IsDeleted)
            {
                return Result.Failure<UpdateSettingResponse>(Error.NullValue, "Setting not found.");
            }

            if (string.Equals(
                    setting.Group,
                    RefundPolicyConstants.Group,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    request.Group.Trim(),
                    RefundPolicyConstants.Group,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<UpdateSettingResponse>(
                    Error.Forbidden,
                    "Refund policy settings must be managed through the refund policy API.");
            }

            setting.Update(
                request.Name.Trim(),
                request.Description?.Trim() ?? string.Empty,
                request.Group.Trim(),
                request.Scope.Trim(),
                request.Value.Trim(),
                request.Type.Trim(),
                currentUserService.UserId);

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            settingRepository.Update(setting);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new UpdateSettingResponse
            {
                Id = setting.Id,
                Code = setting.Code,
                Name = setting.Name,
                Description = setting.Description,
                Group = setting.Group,
                Scope = setting.Scope,
                Value = setting.Value,
                Type = setting.Type
            };

            return Result.Success(response, "Setting updated successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error updating setting {SettingId}", request.Id);
            return Result.Failure<UpdateSettingResponse>(
                Error.ServerError,
                "An error occurred while updating setting.");
        }
    }
}

