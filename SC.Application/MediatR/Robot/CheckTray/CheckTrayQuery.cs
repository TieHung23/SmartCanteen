using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Robot.CheckTray;

/// <summary>
/// Unity (tray-first) quét được mã khay VẬT LÝ → hỏi BE khay đó có HỢP LỆ không
/// (đã đăng ký + đang Available) TRƯỚC KHI pull job. Read-only, KHÔNG reserve.
/// Bind chính thức (atomic reserve) vẫn do <see cref="BindTray.BindTrayCommand"/> làm sau pull.
/// </summary>
public sealed record CheckTrayQuery(string TrayCode) : IQuery<CheckTrayResponse>;

/// <param name="Valid">true = khay đăng ký + Available → được phép pull + bind.</param>
/// <param name="Status">Trạng thái khay ("Available"/"Reserved"/…) hoặc null nếu không tìm thấy.</param>
/// <param name="Reason">Lý do (để Unity log khi không hợp lệ).</param>
public sealed record CheckTrayResponse(bool Valid, string? Status, string Reason);
