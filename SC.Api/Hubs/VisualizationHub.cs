using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using SC.Application.MediatR.Robot.AutoBindTray;
using SC.Application.MediatR.Robot.BindTray;
using SC.Application.MediatR.Robot.CheckTray;
using SC.Application.MediatR.Robot.ClaimServingJob;
using SC.Application.MediatR.Robot.GetServingMap;
using SC.Application.MediatR.Robot.PullNextJob;
using SC.Application.MediatR.Robot.ReportServingStatus;
using SC.Application.MediatR.Robot.RobotHeartbeat;
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
public sealed class VisualizationHub(ISender mediator, IConfiguration config) : Hub
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

    /// <summary>
    /// Unity -&gt; server: kéo job kế tiếp (y hệt Edge pull). null nếu chưa có việc.
    /// <paramref name="trayCode"/> (tray-first, #4): mã khay Unity vừa quét — BE chọn job theo khay
    /// (khay Reserved của job Queued → resume job đó) mà vẫn giữ FIFO. Bỏ trống = FIFO cũ.
    /// </summary>
    public async Task<ServingJobMessage?> PullNextJob(string? trayCode = null)
    {
        var result = await mediator.Send(new PullNextJobCommand(trayCode));
        return result.IsSuccess ? result.Value?.Job : null;
    }

    /// <summary>Unity -&gt; server: "giả quét" mã khay ở đầu quy trình → bind vào job. true nếu thành công.</summary>
    public async Task<bool> BindTray(Guid jobId, string trayCode)
    {
        var result = await mediator.Send(new BindTrayCommand(jobId, trayCode));
        return result.IsSuccess;
    }

    /// <summary>
    /// Unity (tray-first) -&gt; server: kiểm mã khay VẬT LÝ vừa quét có HỢP LỆ không
    /// (đăng ký + Available) TRƯỚC KHI pull job. Read-only, KHÔNG reserve.
    /// Bind atomic (reserve) vẫn do <see cref="BindTray"/> làm sau pull.
    /// </summary>
    public async Task<CheckTrayResponse?> CheckTray(string trayCode)
    {
        var result = await mediator.Send(new CheckTrayQuery(trayCode));
        return result.IsSuccess ? result.Value : null;
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

    /// <summary>
    /// Unity -&gt; server: NHỊP TIM executor (~30s) — các trạm Unity đang lái còn sống.
    /// Dùng CHUNG <see cref="RobotHeartbeatCommand"/> với <see cref="RobotHub"/> (cập nhật
    /// RobotArm.LastHeartbeatUtc, Offline-&gt;Idle). Nhờ đó "mọi arm Offline" = executor VẮNG
    /// mặt thật (đúng cả chế độ Unity-captain), hết false-offline khi Unity rảnh không báo per-lane.
    /// </summary>
    public Task Heartbeat(string[] stations)
        => mediator.Send(new RobotHeartbeatCommand(stations));

    /// <summary>
    /// Unity -&gt; server: "nhận" job của 1 order (<c>Queued</c> -&gt; <c>Pushed</c>) mà KHÔNG qua pull
    /// (bỏ khóa giờ ca), để test bằng JSON cứng (ScenarioPlayer) mà trạng thái job vẫn chạy đúng nấc.
    /// CHỈ hoạt động khi cấu hình <c>Serving:AllowManualClaim = true</c> (bật ở môi trường test/demo);
    /// production mặc định TẮT -&gt; trả false, không đụng gì.
    /// </summary>
    public async Task<bool> ClaimJob(Guid orderId)
    {
        if (!config.GetValue<bool>("Serving:AllowManualClaim"))
            return false;   // khóa: production không cho "nhận" job thủ công
        var result = await mediator.Send(new ClaimServingJobCommand(orderId));
        return result.IsSuccess;
    }
}
