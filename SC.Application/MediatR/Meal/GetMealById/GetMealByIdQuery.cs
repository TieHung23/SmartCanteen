using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Meal.GetMealById;

public class GetMealByIdQuery : IQuery<GetMealByIdResponse>
{
    public Guid Id { get; set; }

    public GetMealByIdQuery(Guid id)
    {
        Id = id;
    }
}
