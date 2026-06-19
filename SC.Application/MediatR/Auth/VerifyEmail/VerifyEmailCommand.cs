using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Auth.VerifyEmail;

public record VerifyEmailCommand(string Email, string Code) : ICommand<VerifyEmailResponse>;
