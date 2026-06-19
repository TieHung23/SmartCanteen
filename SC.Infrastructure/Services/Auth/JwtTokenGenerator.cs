using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SC.Contract.Services.Auth;
using SC.Infrastructure.DependencyInjection.Options;

namespace SC.Infrastructure.Services.Auth;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private const int OpaqueTokenBytes = 48;
    private const int VerificationCodeUpperBound = 1_000_000;

    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new InvalidOperationException(
                "Jwt:SecretKey is not configured. Set it via user-secrets or appsettings.Local.json.");
        }
    }

    public AccessTokenResult GenerateAccessToken(
        Guid userId,
        string email,
        string role,
        bool isVerified)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddYears(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("verified", isVerified ? "true" : "false"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessTokenResult(jwt, expiresAt);
    }

    public OpaqueTokenResult GenerateOpaqueToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(OpaqueTokenBytes);
        var rawToken = Base64UrlEncode(bytes);
        var tokenHash = ComputeSha256(rawToken);
        return new OpaqueTokenResult(rawToken, tokenHash);
    }

    public string HashOpaqueToken(string rawToken)
    {
        return ComputeSha256(rawToken);
    }

    public VerificationCodeResult GenerateEmailVerificationCode()
    {
        var code = RandomNumberGenerator
            .GetInt32(0, VerificationCodeUpperBound)
            .ToString("D6");
        var codeHash = HashVerificationCode(code);
        return new VerificationCodeResult(code, codeHash);
    }

    public string HashVerificationCode(string code)
    {
        return ComputeSha256(code.Trim());
    }

    private static string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
