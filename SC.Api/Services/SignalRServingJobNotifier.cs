using Microsoft.AspNetCore.SignalR;
using SC.Api.Hubs;
using SC.Contract.Services.Robot;

namespace SC.Api.Services;

/// <summary>Đẩy job tới mọi robot service đang kết nối (group "robots") qua event "ReceiveJob".</summary>
public sealed class SignalRServingJobNotifier(IHubContext<RobotHub> hubContext) : IServingJobNotifier
{
    public const string ClientEventName = "ReceiveJob";

    public Task PushJobAsync(ServingJobMessage job, CancellationToken cancellationToken = default)
        => hubContext.Clients
            .Group(RobotHub.RobotGroup)
            .SendAsync(ClientEventName, job, cancellationToken);
}
