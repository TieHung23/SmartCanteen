namespace SC.Infrastructure.DependencyInjection.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "SmartCanteen";

    public string Audience { get; set; } = "SmartCanteenClients";

    public string SecretKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;

    public int EmailVerificationHours { get; set; } = 24;

    public string BaseUrl { get; set; } = "https://localhost:5001";
}
