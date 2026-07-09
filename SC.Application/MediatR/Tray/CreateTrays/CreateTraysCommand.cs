using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Tray.CreateTrays;

/// <summary>
/// Manager đăng ký khay vào pool. 2 kiểu:
/// - Đơn lẻ: Code = "TRAY031".
/// - Hàng loạt: Prefix="TRAY", From=31, To=40 -> TRAY031..TRAY040.
/// </summary>
public sealed record CreateTraysCommand(
    string? Code,
    string? Prefix,
    int? From,
    int? To) : ICommand<CreateTraysResponse>;

public sealed record CreateTraysResponse(IReadOnlyList<string> CreatedCodes, IReadOnlyList<string> SkippedCodes);
