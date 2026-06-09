using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.RefundPolicy.Manager.CreateRefundPolicy;

public sealed class CreateRefundPolicyCommand
    : ICommand<RefundPolicyManagerResponse>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Percent { get; set; }
    public bool RequiresImage { get; set; }
}
