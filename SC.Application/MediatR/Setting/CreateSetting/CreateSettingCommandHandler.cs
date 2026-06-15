using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Application.MediatR.RefundPolicy;
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
            var normalizedGroup = request.Group.Trim();
            var normalizedScope = request.Scope.Trim();

            if (string.Equals(
                    normalizedGroup,
                    RefundPolicyConstants.Group,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<CreateSettingResponse>(
                    Error.Forbidden,
                    "Refund policy settings must be managed through the refund policy API.");
            }

            var existing = await settingRepository.GetQueryable(
                x => !x.IsDeleted
                    && x.Group.ToLower() == normalizedGroup.ToLower()
                    && x.Scope.ToLower() == normalizedScope.ToLower()
                    && x.Code.ToLower() == normalizedCode.ToLower())
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                return Result.Failure<CreateSettingResponse>(
                    Error.InvalidValue,
                    "Setting group, scope, and code already exist.");
            }

            var setting = SettingAggregateRoot.Create(
                normalizedCode,
                request.Name.Trim(),
                request.Description?.Trim() ?? string.Empty,
                normalizedGroup,
                normalizedScope,
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
                Scope = setting.Scope,
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

