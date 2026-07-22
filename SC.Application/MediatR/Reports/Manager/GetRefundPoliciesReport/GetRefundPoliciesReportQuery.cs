using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Reports.Manager.GetRefundPoliciesReport;

public sealed class GetRefundPoliciesReportQuery : IQuery<GetRefundPoliciesReportResponse>
{
    public string? From { get; set; }
    public string? To { get; set; }
}
