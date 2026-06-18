using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Session.GetSessionById;

public class GetSessionByIdQuery : IQuery<GetSessionByIdResponse>
{
    public Guid Id { get; set; }

    public GetSessionByIdQuery(Guid id)
    {
        Id = id;
    }
}
