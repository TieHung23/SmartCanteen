namespace SC.Infrastructure.DependencyInjection.Options;

public sealed class SePayOptions
{
    public const string SectionName = "SePay";

    public string IpnUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public string BankAccountNumber { get; set; } = string.Empty;

    public string BankAccountName { get; set; } = string.Empty;
}
