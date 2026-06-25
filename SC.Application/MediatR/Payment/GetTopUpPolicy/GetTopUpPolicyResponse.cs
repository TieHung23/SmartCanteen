namespace SC.Application.MediatR.Payment.GetTopUpPolicy;

public sealed class GetTopUpPolicyResponse
{
    public decimal VndPerPoint { get; set; }
    public decimal MinTopUpAmount { get; set; }
    public decimal MaxTopUpAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PointName { get; set; } = string.Empty;
}
