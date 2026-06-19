namespace SC.Application.MediatR.Robot.CreateServingJob;

public sealed record CreateServingJobResponse(
    Guid ServingJobId,
    Guid OrderId,
    Guid? TrayId,
    string Status);
