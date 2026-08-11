using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Robot.GetServingMap;

/// <summary>
/// Unity (digital twin) hỏi "bản đồ kệ" của (các) ca ĐANG MỞ: mỗi lane -&gt; món kỳ vọng.
/// Unity dùng để tự dán nhãn tô trên kệ (BowlInfo) cho khớp SlotConfiguration -&gt; verify đúng.
/// Read-only, không đổi trạng thái.
/// </summary>
public sealed record GetServingMapQuery : IQuery<GetServingMapResponse>;
