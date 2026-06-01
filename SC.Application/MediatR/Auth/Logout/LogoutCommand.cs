using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Auth.Logout;

public record LogoutCommand(string RefreshToken) : ICommand<LogoutResponse>;
