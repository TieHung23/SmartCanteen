using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SC.Application.MediatR.Robot.AutoBindTray;
using SC.Application.MediatR.Robot.BindTray;
using SC.Application.MediatR.Robot.GetServingMap;
using SC.Application.MediatR.Robot.PullNextJob;
using SC.Application.MediatR.Robot.ReportServingStatus;
using SC.Contract.Services.Robot;

namespace SC.Api.Hubs;

/// <summary>
/// Hub cho Unity (digital twin) kết nối VÀO. HAI CHIỀU:
/// <list type="bullet">
///   <item>Server ĐẨY sự kiện 1 chiều qua event "ServingEvent"
///         (xem <see cref="SC.Api.Services.SignalRServingVisualizer"/>).</item>
///   <item>Unity GỌI NGƯỢC: <see cref="PullNextJob"/> / <see cref="BindTray"/> /
///         <see cref="ReportStatus"/> — dùng LẠI y hệt command Edge, nên DB cập nhật giống nhau
///         bất kể ai gắp (Unity demo hay robot thật).</item>
/// </list>
/// Unity xác thực bằng JWT login-token như client thường.
/// Chế độ "Executor" (demo): Unity gọi ngược để ghi DB. Chế độ "Mirror" (robot thật):
/// Unity chỉ nghe "ServingEvent", KHÔNG gọi ngược (để Edge là nguồn report duy nhất).
/// </summary>
[Authorize]
public sealed class VisualizationHub(ISender mediator) : Hub
{
    public const string VizGroup = "visualization";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, VizGroup);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, VizGroup);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Unity -&gt; server: kéo job kế tiếp (y hệt Edge pull). null nếu chưa có việc.</summary>
    public async Task<ServingJobMessage?> PullNextJob()
    {
        var result = await mediator.Send(new PullNextJobCommand());
        return result.IsSuccess ? result.Value?.Job : null;
    }

    /// <summary>Unity -&gt; server: "giả quét" mã khay ở đầu quy trình → bind vào job. true nếu thành công.</summary>
    public async Task<bool> BindTray(Guid jobId, string trayCode)
    {
        var result = await mediator.Send(new BindTrayCommand(jobId, trayCode));
        return result.IsSuccess;
    }

    /// <summary>
    /// Unity -&gt; server: KHÔNG có webcam → BE TỰ CHỌN 1 khay Available bất kỳ rồi bind. true nếu
    /// bind được (hoặc job đã có khay); false nếu HẾT khay → Unity KHÔNG phục vụ (giống Edge HoldForStaff).
    /// </summary>
    public async Task<bool> AutoBindTray(Guid jobId)
    {
        var result = await mediator.Send(new AutoBindTrayCommand(jobId));
        return result.IsSuccess;
    }

    /// <summary>
    /// Unity -&gt; server: lấy "bản đồ kệ" ca đang mở (lane -&gt; món kỳ vọng) để tự dán nhãn tô
    /// (BowlInfo) cho khớp SlotConfiguration. Rỗng nếu không có ca mở.
    /// </summary>
    public async Task<GetServingMapResponse?> GetServingMap()
    {
        var result = await mediator.Send(new GetServingMapQuery());
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>Unity -&gt; server: báo tiến độ/kết quả 1 bước (y hệt Edge ReportStatus) → ghi DB.</summary>
    public Task ReportStatus(ServingStatusUpdate update)
        => mediator.Send(new ReportServingStatusCommand(
            update.OrderId,
            update.State,
            update.Station,
            update.Message,
            update.TrayId,
            update.DishId));
}
