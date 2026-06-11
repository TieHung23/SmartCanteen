using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SettingAggregate = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Application.MediatR.RefundPolicy.Manager.DeleteRefundPolicy;

internal sealed class DeleteRefundPolicyCommandHandler(
    IGenericRepository<SettingAggregate, Guid> settingRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<DeleteRefundPolicyCommandHandler> logger)
    : ICommandHandler<DeleteRefundPolicyCommand, string>
{
    public async Task<Result<string>> Handle(
        DeleteRefundPolicyCommand request,
        CancellationToken cancellationToken)
    {
        var scope = request.Code.Trim().ToUpperInvariant();

        try
        {
            var settings = await settingRepository
                .GetQueryable(setting =>
                    !setting.IsDeleted
                    && setting.Group == RefundPolicyConstants.Group
                    && setting.Scope == scope)
                .ToListAsync(cancellationToken);

            if (settings.Count == 0)
            {
                return Result.Failure<string>(
                    Error.NullValue,
                    "Refund policy not found.");
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            foreach (var setting in settings)
            {
                setting.SoftDelete(currentUserService.UserId);
                settingRepository.Update(setting);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(scope, "Refund policy deleted successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error deleting refund policy {PolicyScope}", scope);
            return Result.Failure<string>(
                Error.ServerError,
                "An error occurred while deleting the refund policy.");
        }
    }
}
