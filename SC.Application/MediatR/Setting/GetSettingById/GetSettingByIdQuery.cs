using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Setting.GetSettingById;

public class GetSettingByIdQuery : IQuery<GetSettingByIdResponse>
{
    public Guid Id { get; set; }

    public GetSettingByIdQuery()
    {
    }

    public GetSettingByIdQuery(Guid id)
    {
        Id = id;
    }
}

