namespace SC.Application.MediatR.RefundPolicy.GetActiveRefundPolicies;

public sealed class GetActiveRefundPoliciesResponse
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Percent { get; set; }
    public bool RequiresImage { get; set; }
}
