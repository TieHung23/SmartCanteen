using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Reports.Manager.GetOrderIssuesReport;

public sealed class GetOrderIssuesReportQuery : IQuery<GetOrderIssuesReportResponse>
{
    public string? From { get; set; }
    public string? To { get; set; }
}
