using SC.Contract.Services.Robot;

namespace SC.Application.MediatR.Robot.PullNextJob;

/// <summary>Job được giao cho robot (null nếu chưa có việc / hết khay → controller trả 204).</summary>
public sealed record PullNextJobResponse(ServingJobMessage? Job);
