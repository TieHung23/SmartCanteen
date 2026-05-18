using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Setting.GetAllSettings;

public class GetAllSettingsQuery : PaginationParams, IQuery<PaginatedList<GetAllSettingsResponse>>
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Group { get; set; }
    public string? Type { get; set; }

    public GetAllSettingsQuery()
    {
    }

    public GetAllSettingsQuery(
        int pageNumber = 1,
        int pageSize = 10,
        string? code = null,
        string? name = null,
        string? group = null,
        string? type = null)
        : base(pageNumber, pageSize)
    {
        Code = code;
        Name = name;
        Group = group;
        Type = type;
    }
}

