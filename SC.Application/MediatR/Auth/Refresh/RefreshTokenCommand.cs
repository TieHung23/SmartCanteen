using SC.Application.MediatR.Auth.Shared;
using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Auth.Refresh;

public record RefreshTokenCommand(string RefreshToken) : ICommand<AuthTokensDto>;
