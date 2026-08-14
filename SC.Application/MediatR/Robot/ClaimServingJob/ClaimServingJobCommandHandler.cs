using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.ServingJob.Enum;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;

namespace SC.Application.MediatR.Robot.ClaimServingJob;

internal sealed class ClaimServingJobCommandHandler(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<ClaimServingJobCommandHandler> logger
) : ICommandHandler<ClaimServingJobCommand, ClaimServingJobResponse>
{
    public async Task<Result<ClaimServingJobResponse>> Handle(
        ClaimServingJobCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserService.UserId;

        try
        {
            // Job Queued cũ nhất của order (bỏ job đã hủy/đã lấy/đã đẩy). Chỉ "nhận" khi ĐANG Queued
            // -> không giành job mà robot thật đã Pushed, không đụng job đang chạy.
            var jobs = await servingJobRepository.FindListAsync(
                x => x.OrderId == request.OrderId
                     && x.Status == ServingJobStatus.Queued
                     && !x.IsDeleted,
                cancellationToken);
            var job = jobs.OrderBy(x => x.CreatedAtUtc).FirstOrDefault();

            if (job is null)
            {
                return Result.Failure<ClaimServingJobResponse>(
                    Error.ServingJobNotFound, "Không có công việc phục vụ đang chờ cho đơn này.");
            }

            // Queued -> Pushed (bỏ qua bước pull có khóa giờ ca). KHÔNG gán khay ở đây.
            job.MarkPushed(actorId);
            servingJobRepository.Update(job);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new ClaimServingJobResponse(job.Id, job.OrderId, job.Status.ToString()),
                "Đã nhận công việc (Queued → Pushed).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error claiming serving job for order {OrderId}", request.OrderId);
            return Result.Failure<ClaimServingJobResponse>(
                Error.ServerError, "Đã xảy ra lỗi khi nhận công việc phục vụ.");
        }
    }
}
