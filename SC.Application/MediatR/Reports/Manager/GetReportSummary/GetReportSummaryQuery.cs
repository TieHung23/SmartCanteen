using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Reports.Manager.GetReportSummary;

public sealed class GetReportSummaryQuery : IQuery<GetReportSummaryResponse>
{
    public string? From { get; set; }
    public string? To { get; set; }
}
