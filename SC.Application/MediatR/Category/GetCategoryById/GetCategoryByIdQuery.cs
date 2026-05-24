using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Category.GetCategoryById;

public class GetCategoryByIdQuery : IQuery<GetCategoryByIdResponse>
{
    public Guid Id { get; set; }

    public GetCategoryByIdQuery()
    {
    }

    public GetCategoryByIdQuery(Guid id)
    {
        Id = id;
    }
}
