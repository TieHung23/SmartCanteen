using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SettingAggregateRoot = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Application.MediatR.RefundPolicy.GetActiveRefundPolicies;

internal sealed class GetActiveRefundPoliciesQueryHandler(
    IGenericRepository<SettingAggregateRoot, Guid> settingRepository,
    ILogger<GetActiveRefundPoliciesQueryHandler> logger)
    : IQueryHandler<GetActiveRefundPoliciesQuery, IReadOnlyList<GetActiveRefundPoliciesResponse>>
{
    public async Task<Result<IReadOnlyList<GetActiveRefundPoliciesResponse>>> Handle(
        GetActiveRefundPoliciesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await settingRepository
                .FindListAsync(setting =>
                    !setting.IsDeleted
                    && setting.Group.ToUpper() == RefundPolicyConstants.Group,
                    cancellationToken);

            var policies = new List<GetActiveRefundPoliciesResponse>();

            foreach (var scopeGroup in settings.GroupBy(
                         setting => setting.Scope,
                         StringComparer.OrdinalIgnoreCase))
            {
                if (!RefundPolicyDefinition.TryCreate(
                        scopeGroup.Key,
                        scopeGroup,
                        out var policy))
                {
                    logger.LogWarning(
                        "Refund policy scope {PolicyScope} is incomplete or invalid and was skipped.",
                        scopeGroup.Key);
                    continue;
                }

                policies.Add(new GetActiveRefundPoliciesResponse
                {
                    Code = policy!.Scope,
                    Name = policy.Name,
                    Description = policy.Description,
                    Percent = policy!.Percent,
                    RequiresImage = policy.RequiresImage
                });
            }

            policies = policies.OrderBy(policy => policy.Name).ToList();

            return Result.Success<IReadOnlyList<GetActiveRefundPoliciesResponse>>(
                policies,
                "Refund policies retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving refund policies");
            return Result.Failure<IReadOnlyList<GetActiveRefundPoliciesResponse>>(
                Error.ServerError,
                "An error occurred while retrieving refund policies.");
        }
    }
}
