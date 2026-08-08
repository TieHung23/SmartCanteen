using SC.Application.MediatR.Session.FinalizeSession;
using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Session.FinalizeSessionNow;

/// <summary>
/// Demo/ops variant of FinalizeSession: finalizes the session AND pulls its serving
/// window (AvailableFrom) forward to now, so already-queued serving jobs become
/// eligible for robot pickup immediately instead of waiting for the scheduled time.
/// </summary>
public class FinalizeSessionNowCommand : ICommand<FinalizeSessionResponse>
{
    public Guid SessionId { get; set; }
    public List<PreparedDishDto> PreparedDishes { get; set; } = [];
}
