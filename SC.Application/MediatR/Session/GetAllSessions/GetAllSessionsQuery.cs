using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Session.GetAllSessions;

public class GetAllSessionsQuery : PaginationParams, IQuery<PaginatedList<GetAllSessionsResponse>>
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }

    public GetAllSessionsQuery()
    {
    }

    public GetAllSessionsQuery(int pageNumber = 1, int pageSize = 10, string? name = null, bool? isActive = null)
        : base(pageNumber, pageSize)
    {
        Name = name;
        IsActive = isActive;
    }
}
