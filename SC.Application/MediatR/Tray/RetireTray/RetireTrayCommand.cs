using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Tray.RetireTray;

/// <summary>Khay hỏng -> rút khỏi pool (soft-delete). Chỉ khi khay đang Available.</summary>
public sealed record RetireTrayCommand(Guid Id) : ICommand<RetireTrayResponse>;

public sealed record RetireTrayResponse(Guid Id, string Code);
