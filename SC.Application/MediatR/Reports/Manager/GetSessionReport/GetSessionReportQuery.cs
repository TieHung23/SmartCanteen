using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Reports.Manager.GetSessionReport;

public sealed class GetSessionReportQuery : IQuery<GetSessionReportResponse>
{
    public string? From { get; set; }
    public string? To { get; set; }
}
