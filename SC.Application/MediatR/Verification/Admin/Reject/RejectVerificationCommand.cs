using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Verification.Admin.Reject;

public record RejectVerificationCommand(Guid Id, string Reason) : ICommand<RejectVerificationResponse>;
