using SC.Application.MediatR.Auth.Shared;
using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Auth.Login;

public record LoginCommand(string Email, string Password) : ICommand<AuthTokensDto>;
