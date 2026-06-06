using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SettingAggregateRoot = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Application.MediatR.Setting.DeleteSetting;

internal class DeleteSettingCommandHandler(
    IGenericRepository<SettingAggregateRoot, Guid> settingRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<DeleteSettingCommandHandler> logger
) : ICommandHandler<DeleteSettingCommand, DeleteSettingResponse>
{
    public async Task<Result<DeleteSettingResponse>> Handle(
        DeleteSettingCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var setting = await settingRepository.GetByIdAsync(request.Id, cancellationToken);

            if (setting is null || setting.IsDeleted)
            {
                return Result.Failure<DeleteSettingResponse>(
                    Error.NullValue,
                    "Setting not found.");
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            setting.SoftDelete(currentUserService.UserId);
            settingRepository.Update(setting);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new DeleteSettingResponse
            {
                Id = request.Id,
                Message = "Setting deleted successfully."
            };

            return Result.Success(response, response.Message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error deleting setting {SettingId}", request.Id);
            return Result.Failure<DeleteSettingResponse>(
                Error.ServerError,
                "An error occurred while deleting setting.");
        }
    }
}

