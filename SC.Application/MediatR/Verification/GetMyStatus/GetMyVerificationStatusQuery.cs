using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Verification.GetMyStatus;

public record GetMyVerificationStatusQuery() : IQuery<VerificationStatusResponse>;
