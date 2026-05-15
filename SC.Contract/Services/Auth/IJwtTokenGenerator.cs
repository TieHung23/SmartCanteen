namespace SC.Contract.Services.Auth;

public record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

public record OpaqueTokenResult(string RawToken, string TokenHash);

public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(
        Guid userId,
        string email,
        string role,
        bool isVerified);

    OpaqueTokenResult GenerateOpaqueToken();
}
