using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Robot;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.ServingJob.Enum;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;

namespace SC.Application.MediatR.ServingJobAdmin.RequeueServingJob;

internal sealed class RequeueServingJobCommandHandler(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IServingJobNotifier servingJobNotifier,
    ILogger<RequeueServingJobCommandHandler> logger
) : ICommandHandler<RequeueServingJobCommand, RequeueServingJobResponse>
{
    public async Task<Result<RequeueServingJobResponse>> Handle(
        RequeueServingJobCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var job = await servingJobRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (job is null)
            {
                return Result.Failure<RequeueServingJobResponse>(
                    Error.ServingJobNotFound, "Serving job was not found.");
            }

            if (job.Status != ServingJobStatus.Failed)
            {
                return Result.Failure<RequeueServingJobResponse>(
                    Error.ServingJobNotReady,
                    $"Only failed jobs can be requeued (current: {job.Status}).");
            }

            job.Requeue(currentUserService.UserId);
            servingJobRepository.Update(job);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // đánh thức robot pull lại (best-effort)
            try { await servingJobNotifier.PingNewJobAsync(cancellationToken); }
            catch (Exception ex) { logger.LogError(ex, "Ping after requeue failed"); }

            return Result.Success(
                new RequeueServingJobResponse(job.Id, job.OrderId, job.Status.ToString()),
                "Job requeued; robots pinged.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error requeuing serving job {Id}", request.Id);
            return Result.Failure<RequeueServingJobResponse>(
                Error.ServerError, "An error occurred while requeuing the job.");
        }
    }
}
