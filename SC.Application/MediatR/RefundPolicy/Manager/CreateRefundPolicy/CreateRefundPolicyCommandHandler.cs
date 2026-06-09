using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SettingAggregate = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Application.MediatR.RefundPolicy.Manager.CreateRefundPolicy;

internal sealed class CreateRefundPolicyCommandHandler(
    IGenericRepository<SettingAggregate, Guid> settingRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<CreateRefundPolicyCommandHandler> logger)
    : ICommandHandler<CreateRefundPolicyCommand, RefundPolicyManagerResponse>
{
    public async Task<Result<RefundPolicyManagerResponse>> Handle(
        CreateRefundPolicyCommand request,
        CancellationToken cancellationToken)
    {
        var scope = request.Code.Trim().ToUpperInvariant();

        try
        {
            var exists = await settingRepository
                .GetQueryable(setting =>
                    !setting.IsDeleted
                    && setting.Group == RefundPolicyConstants.Group
                    && setting.Scope == scope)
                .AnyAsync(cancellationToken);

            if (exists)
            {
                return Result.Failure<RefundPolicyManagerResponse>(
                    Error.InvalidValue,
                    "Refund policy code already exists.");
            }

            var settings = CreateSettings(request, scope, currentUserService.UserId);

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            foreach (var setting in settings)
            {
                await settingRepository.AddAsync(setting, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(
                ToResponse(request, scope),
                "Refund policy created successfully.");
        }
        catch (DbUpdateException ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogWarning(ex, "Duplicate refund policy scope {PolicyScope}", scope);
            return Result.Failure<RefundPolicyManagerResponse>(
                Error.InvalidValue,
                "Refund policy code already exists.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error creating refund policy {PolicyScope}", scope);
            return Result.Failure<RefundPolicyManagerResponse>(
                Error.ServerError,
                "An error occurred while creating the refund policy.");
        }
    }

    private static IReadOnlyList<SettingAggregate> CreateSettings(
        CreateRefundPolicyCommand request,
        string scope,
        Guid userId)
    {
        return
        [
            SettingAggregate.Create(
                RefundPolicyConstants.NameCode,
                "Policy name",
                "Display name of the refund policy.",
                RefundPolicyConstants.Group,
                scope,
                request.Name.Trim(),
                RefundPolicyConstants.StringType,
                userId),
            SettingAggregate.Create(
                RefundPolicyConstants.DescriptionCode,
                "Policy description",
                "Description of the refund policy.",
                RefundPolicyConstants.Group,
                scope,
                request.Description.Trim(),
                RefundPolicyConstants.StringType,
                userId),
            SettingAggregate.Create(
                RefundPolicyConstants.PercentCode,
                "Refund percent",
                "Refund percentage for this policy.",
                RefundPolicyConstants.Group,
                scope,
                request.Percent.ToString(CultureInfo.InvariantCulture),
                RefundPolicyConstants.DecimalType,
                userId),
            SettingAggregate.Create(
                RefundPolicyConstants.RequiresImageCode,
                "Requires image",
                "Whether evidence images are required.",
                RefundPolicyConstants.Group,
                scope,
                request.RequiresImage.ToString().ToLowerInvariant(),
                RefundPolicyConstants.BooleanType,
                userId)
        ];
    }

    private static RefundPolicyManagerResponse ToResponse(
        CreateRefundPolicyCommand request,
        string scope)
    {
        return new RefundPolicyManagerResponse
        {
            Code = scope,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Percent = request.Percent,
            RequiresImage = request.RequiresImage
        };
    }
}
