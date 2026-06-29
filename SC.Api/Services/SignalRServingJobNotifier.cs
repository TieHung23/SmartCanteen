using Microsoft.AspNetCore.SignalR;
using SC.Api.Hubs;
using SC.Contract.Services.Robot;

namespace SC.Api.Services;

/// <summary>Đẩy job tới mọi robot service đang kết nối (group "robots") qua event "ReceiveJob".</summary>
public sealed class SignalRServingJobNotifier(IHubContext<RobotHub> hubContext) : IServingJobNotifier
{
    public const string ClientEventName = "ReceiveJob";        // PUSH cũ: đẩy kèm data
    public const string PingEventName = "JobAvailable";        // HYBRID: ping đánh thức (không data)

    public Task PushJobAsync(ServingJobMessage job, CancellationToken cancellationToken = default)
        => hubContext.Clients
            .Group(RobotHub.RobotGroup)
            .SendAsync(ClientEventName, job, cancellationToken);

    public Task PingNewJobAsync(CancellationToken cancellationToken = default)
        => hubContext.Clients
            .Group(RobotHub.RobotGroup)
            .SendAsync(PingEventName, cancellationToken);
}
