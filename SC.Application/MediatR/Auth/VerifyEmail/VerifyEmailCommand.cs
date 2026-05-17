using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Auth.VerifyEmail;

public record VerifyEmailCommand(string Token) : ICommand;
