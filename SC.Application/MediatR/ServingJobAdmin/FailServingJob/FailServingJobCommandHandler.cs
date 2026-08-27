using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.RobotEventLog.Enum;
using SC.Domain.Domain.ServingJob.Enum;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using RobotEventLogEntity = SC.Domain.Domain.RobotEventLog.Entity.RobotEventLog;

namespace SC.Application.MediatR.ServingJobAdmin.FailServingJob;

internal sealed class FailServingJobCommandHandler(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<RobotEventLogEntity, Guid> robotEventLogRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<FailServingJobCommandHandler> logger
) : ICommandHandler<FailServingJobCommand, FailServingJobResponse>
{
    public async Task<Result<FailServingJobResponse>> Handle(
        FailServingJobCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserService.UserId;

        try
        {
            var job = await servingJobRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (job is null)
            {
                return Result.Failure<FailServingJobResponse>(
                    Error.ServingJobNotFound, "Serving job was not found.");
            }

            // Chỉ job ĐANG DANG DỞ mới cho staff tuyên bố Failed. Đã Failed/Cancelled/OnShelf/Collected -> chặn
            // (idempotent + không "hồi sinh" job đã kết thúc).
            if (job.Status is not (ServingJobStatus.Queued
                or ServingJobStatus.Pushed
                or ServingJobStatus.Assembling))
            {
                return Result.Failure<FailServingJobResponse>(
                    Error.ServingJobNotReady,
                    $"Chỉ job đang dang dở (Queued/Pushed/Assembling) mới có thể đánh lỗi (hiện tại: {job.Status}).");
            }

            var reason = string.IsNullOrWhiteSpace(request.Reason)
                ? "Staff đánh dấu đơn lỗi để xử lý thủ công."
                : request.Reason.Trim();

            job.MarkFailed(reason, actorId);
            servingJobRepository.Update(job);

            // audit: phân biệt "NGƯỜI (staff) tuyên bố Failed" với executor tự báo lỗi (BR dashboard).
            await robotEventLogRepository.AddAsync(
                RobotEventLogEntity.Create(
                    RobotEventType.Error,
                    actorId,
                    servingJobId: job.Id,
                    orderId: job.OrderId,
                    message: $"MANUAL fail by staff: {reason}"),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new FailServingJobResponse(job.Id, job.OrderId, job.Status.ToString()),
                "Đã đánh dấu job lỗi; giờ có thể Requeue hoặc ManualComplete.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error failing serving job {Id}", request.Id);
            return Result.Failure<FailServingJobResponse>(
                Error.ServerError, "Đã xảy ra lỗi khi đánh dấu job lỗi.");
        }
    }
}
