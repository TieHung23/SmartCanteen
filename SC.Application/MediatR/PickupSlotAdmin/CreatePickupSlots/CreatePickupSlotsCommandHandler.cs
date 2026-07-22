using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using PickupSlotEntity = SC.Domain.Domain.PickupSlot.Entity.PickupSlot;

namespace SC.Application.MediatR.PickupSlotAdmin.CreatePickupSlots;

internal sealed class CreatePickupSlotsCommandHandler(
    IGenericRepository<PickupSlotEntity, Guid> pickupSlotRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<CreatePickupSlotsCommandHandler> logger
) : ICommandHandler<CreatePickupSlotsCommand, CreatePickupSlotsResponse>
{
    private const int MaxBulk = 100;

    public async Task<Result<CreatePickupSlotsResponse>> Handle(
        CreatePickupSlotsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var wanted = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                wanted.Add(request.Code.Trim());
            }
            else if (!string.IsNullOrWhiteSpace(request.Prefix)
                     && request.From is int from && request.To is int to
                     && from >= 0 && to >= from && to - from + 1 <= MaxBulk)
            {
                var prefix = request.Prefix.Trim();
                for (int i = from; i <= to; i++)
                    wanted.Add($"{prefix}{i:D2}");
            }

            if (wanted.Count == 0)
            {
                return Result.Failure<CreatePickupSlotsResponse>(
                    Error.InvalidValue,
                    $"Provide Code, or Prefix+From+To (max {MaxBulk} per request).");
            }

            var existing = await pickupSlotRepository.FindListAsync(
                x => wanted.Contains(x.Code), cancellationToken);
            var existingCodes = existing.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var created = new List<string>();
            var skipped = new List<string>();
            foreach (var code in wanted)
            {
                if (existingCodes.Contains(code)) { skipped.Add(code); continue; }
                await pickupSlotRepository.AddAsync(
                    PickupSlotEntity.Create(code, currentUserService.UserId), cancellationToken);
                created.Add(code);
            }

            if (created.Count > 0)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new CreatePickupSlotsResponse(created, skipped),
                $"Created {created.Count} slot(s); skipped {skipped.Count} duplicate(s).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating pickup slots");
            return Result.Failure<CreatePickupSlotsResponse>(
                Error.ServerError, "An error occurred while creating pickup slots.");
        }
    }
}
