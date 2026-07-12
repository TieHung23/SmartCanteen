using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.RobotEventLog.Enum;
using SC.Domain.Domain.ServingJob.Enum;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using RobotEventLogEntity = SC.Domain.Domain.RobotEventLog.Entity.RobotEventLog;

namespace SC.Application.MediatR.ServingJobAdmin.ManualCompleteServingJob;

internal sealed class ManualCompleteServingJobCommandHandler(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<RobotEventLogEntity, Guid> robotEventLogRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<ManualCompleteServingJobCommandHandler> logger
) : ICommandHandler<ManualCompleteServingJobCommand, ManualCompleteServingJobResponse>
{
    public async Task<Result<ManualCompleteServingJobResponse>> Handle(
        ManualCompleteServingJobCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserService.UserId;

        try
        {
            var job = await servingJobRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (job is null)
            {
                return Result.Failure<ManualCompleteServingJobResponse>(
                    Error.ServingJobNotFound, "Serving job was not found.");
            }

            if (job.Status != ServingJobStatus.Failed)
            {
                return Result.Failure<ManualCompleteServingJobResponse>(
                    Error.ServingJobNotReady,
                    $"Only failed jobs can be manually completed (current: {job.Status}).");
            }

            job.MarkAssembledManually(actorId);
            servingJobRepository.Update(job);

            // audit: phân biệt thao tác TAY của staff với robot tự phục vụ (BR dashboard)
            await robotEventLogRepository.AddAsync(
                RobotEventLogEntity.Create(
                    RobotEventType.Recovered,
                    actorId,
                    servingJobId: job.Id,
                    orderId: job.OrderId,
                    message: "MANUAL completion by staff" +
                             (string.IsNullOrWhiteSpace(request.Note) ? "" : $": {request.Note.Trim()}")),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new ManualCompleteServingJobResponse(job.Id, job.OrderId, job.Status.ToString()),
                "Job marked as manually assembled; tray can now be shelved.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error manually completing serving job {Id}", request.Id);
            return Result.Failure<ManualCompleteServingJobResponse>(
                Error.ServerError, "An error occurred while completing the job.");
        }
    }
}
