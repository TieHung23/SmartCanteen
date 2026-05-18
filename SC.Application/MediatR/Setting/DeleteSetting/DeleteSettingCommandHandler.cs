using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SettingAggregateRoot = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Application.MediatR.Setting.DeleteSetting;

internal class DeleteSettingCommandHandler(
    IRepositoryBase<SettingAggregateRoot, Guid> settingRepository,
    ICurrentUserService currentUserService,
    ILogger<DeleteSettingCommandHandler> logger
) : ICommandHandler<DeleteSettingCommand>
{
    public async Task<Result> Handle(
        DeleteSettingCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var setting = await settingRepository.FindByIdAsync(request.Id, cancellationToken);

            if (setting is null || setting.IsDeleted)
            {
                return Result.Failure(Error.NullValue, "Setting not found.");
            }

            setting.SoftDelete(currentUserService.UserId);

            var deleteResult = await settingRepository.UpdateAsync(setting);
            if (deleteResult.IsFailure)
            {
                return Result.Failure(
                    deleteResult.Error ?? Error.ServerError,
                    deleteResult.Message);
            }

            return Result.Success("Setting deleted successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting setting {SettingId}", request.Id);
            return Result.Failure(
                Error.ServerError,
                "An error occurred while deleting setting.");
        }
    }
}

