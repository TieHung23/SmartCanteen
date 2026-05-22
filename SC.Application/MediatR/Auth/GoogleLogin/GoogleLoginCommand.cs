using SC.Application.MediatR.Auth.Shared;
using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Auth.GoogleLogin;

/// <summary>
/// Signs a user in with a Google ID token obtained by the frontend from Google Identity
/// Services. Only FPT University accounts are accepted.
/// </summary>
public record GoogleLoginCommand(string IdToken) : ICommand<AuthTokensDto>;
