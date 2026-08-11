namespace SC.Application.MediatR.Robot.AutoBindTray;

/// <summary>Kết quả auto-bind: khay nào đã gán (null nếu job đã có khay sẵn / hết khay).</summary>
public sealed record AutoBindTrayResponse(Guid JobId, Guid? TrayId, string? TrayCode);
