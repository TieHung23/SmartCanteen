using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Dish.GetDishById;

public class GetDishByIdQuery : IQuery<GetDishByIdResponse>
{
    public Guid Id { get; set; }

    public GetDishByIdQuery()
    {
    }

    public GetDishByIdQuery(Guid id)
    {
        Id = id;
    }
}
