using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Verification.SubmitVerification;

public record SubmitVerificationCommand(
    IReadOnlyList<VerificationFileInput> Files) : ICommand<Guid>;
