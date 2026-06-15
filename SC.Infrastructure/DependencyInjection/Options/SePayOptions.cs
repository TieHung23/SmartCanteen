namespace SC.Infrastructure.DependencyInjection.Options;

public sealed class SePayOptions
{
    public const string SectionName = "SePay";

    public string IpnUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    public int WebhookTimestampToleranceSeconds { get; set; } = 300;

    public string BankName { get; set; } = string.Empty;

    public string BankAccountNumber { get; set; } = string.Empty;

    public string BankAccountName { get; set; } = string.Empty;

    public string PaymentCodePrefix { get; set; } = "SC";

    public string RequiredTransferContentPrefix { get; set; } = string.Empty;

    public string QrTemplate { get; set; } = "compact";
}
