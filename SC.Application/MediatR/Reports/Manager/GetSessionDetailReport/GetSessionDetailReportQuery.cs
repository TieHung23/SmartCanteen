using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Reports.Manager.GetSessionDetailReport;

public sealed class GetSessionDetailReportQuery(Guid sessionId) : IQuery<GetSessionDetailReportResponse>
{
    public Guid SessionId { get; } = sessionId;
}
