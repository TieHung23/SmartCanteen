using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Session.GetSessionDishQuantities;

public class GetSessionDishQuantitiesQuery : IQuery<GetSessionDishQuantitiesResponse>
{
    public Guid SessionId { get; set; }

    public GetSessionDishQuantitiesQuery(Guid sessionId)
    {
        SessionId = sessionId;
    }
}
