namespace SC.Contract.Services.Auth;

public record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

public record OpaqueTokenResult(string RawToken, string TokenHash);

public record VerificationCodeResult(string Code, string CodeHash);

public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(
        Guid userId,
        string email,
        string role,
        bool isVerified);

    OpaqueTokenResult GenerateOpaqueToken();

    string HashOpaqueToken(string rawToken);

    VerificationCodeResult GenerateEmailVerificationCode();

    string HashVerificationCode(string code);
}
