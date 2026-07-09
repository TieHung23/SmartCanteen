using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;

namespace SC.Application.MediatR.Tray.CreateTrays;

internal sealed class CreateTraysCommandHandler(
    IGenericRepository<TrayEntity, Guid> trayRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<CreateTraysCommandHandler> logger
) : ICommandHandler<CreateTraysCommand, CreateTraysResponse>
{
    private const int MaxBulk = 100;

    public async Task<Result<CreateTraysResponse>> Handle(
        CreateTraysCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // build danh sách code muốn tạo (đơn lẻ hoặc dải)
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
                    wanted.Add($"{prefix}{i:D3}");
            }

            if (wanted.Count == 0)
            {
                return Result.Failure<CreateTraysResponse>(
                    Error.InvalidValue,
                    $"Provide Code, or Prefix+From+To (max {MaxBulk} per request).");
            }

            // idempotent: code trùng (kể cả khay đã soft-delete giữ code) -> bỏ qua, không lỗi
            var existing = await trayRepository.FindListAsync(
                x => wanted.Contains(x.Code), cancellationToken);
            var existingCodes = existing.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var created = new List<string>();
            var skipped = new List<string>();
            foreach (var code in wanted)
            {
                if (existingCodes.Contains(code)) { skipped.Add(code); continue; }
                await trayRepository.AddAsync(
                    TrayEntity.Create(code, currentUserService.UserId), cancellationToken);
                created.Add(code);
            }

            if (created.Count > 0)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new CreateTraysResponse(created, skipped),
                $"Created {created.Count} tray(s); skipped {skipped.Count} duplicate(s).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating trays");
            return Result.Failure<CreateTraysResponse>(
                Error.ServerError, "An error occurred while creating trays.");
        }
    }
}
