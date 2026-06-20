using System.Globalization;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SettingAggregate = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Application.MediatR.RefundPolicy.Manager.UpdateRefundPolicy;

internal sealed class UpdateRefundPolicyCommandHandler(
    IGenericRepository<SettingAggregate, Guid> settingRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<UpdateRefundPolicyCommandHandler> logger)
    : ICommandHandler<UpdateRefundPolicyCommand, RefundPolicyManagerResponse>
{
    public async Task<Result<RefundPolicyManagerResponse>> Handle(
        UpdateRefundPolicyCommand request,
        CancellationToken cancellationToken)
    {
        var scope = request.Code.Trim().ToUpperInvariant();

        try
        {
            var settings = await settingRepository
                .FindListAsync(setting =>
                    !setting.IsDeleted
                    && setting.Group == RefundPolicyConstants.Group
                    && setting.Scope == scope,
                    cancellationToken);

            if (settings.Count == 0)
            {
                return Result.Failure<RefundPolicyManagerResponse>(
                    Error.NullValue,
                    "Refund policy not found.");
            }

            if (!RefundPolicyDefinition.TryCreate(scope, settings, out _))
            {
                return Result.Failure<RefundPolicyManagerResponse>(
                    Error.InvalidValue,
                    "Refund policy data is incomplete or invalid.");
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [RefundPolicyConstants.NameCode] = request.Name.Trim(),
                [RefundPolicyConstants.DescriptionCode] = request.Description.Trim(),
                [RefundPolicyConstants.PercentCode] =
                    request.Percent.ToString(CultureInfo.InvariantCulture),
                [RefundPolicyConstants.RequiresImageCode] =
                    request.RequiresImage.ToString().ToLowerInvariant()
            };

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            foreach (var setting in settings)
            {
                if (!values.TryGetValue(setting.Code, out var value))
                {
                    continue;
                }

                setting.Update(
                    setting.Name,
                    setting.Description,
                    value,
                    setting.Type,
                    currentUserService.UserId);
                settingRepository.Update(setting);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(
                new RefundPolicyManagerResponse
                {
                    Code = scope,
                    Name = request.Name.Trim(),
                    Description = request.Description.Trim(),
                    Percent = request.Percent,
                    RequiresImage = request.RequiresImage
                },
                "Refund policy updated successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error updating refund policy {PolicyScope}", scope);
            return Result.Failure<RefundPolicyManagerResponse>(
                Error.ServerError,
                "An error occurred while updating the refund policy.");
        }
    }
}
