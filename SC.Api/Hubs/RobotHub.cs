using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SC.Application.MediatR.Robot.ReportServingStatus;
using SC.Contract.Services.Robot;

namespace SC.Api.Hubs;

/// <summary>
/// Hub cho robot-arm-service (Python) kết nối VÀO. Server đẩy job qua event "ReceiveJob"
/// (xem <see cref="SC.Api.Services.SignalRServingJobNotifier"/>); robot gọi ngược "ReportStatus".
/// Robot service xác thực bằng JWT service-account (giống các client khác).
/// </summary>
[Authorize]
public sealed class RobotHub(ISender mediator) : Hub
{
    public const string RobotGroup = "robots";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, RobotGroup);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, RobotGroup);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>robot service -&gt; server: báo tiến độ/ kết quả 1 job.</summary>
    public Task ReportStatus(ServingStatusUpdate update)
        => mediator.Send(new ReportServingStatusCommand(
            update.OrderId,
            update.State,
            update.Station,
            update.Message,
            update.TrayId));
}
