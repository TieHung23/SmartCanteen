using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Verification.Admin.Approve;

public record ApproveVerificationCommand(Guid Id) : ICommand;
