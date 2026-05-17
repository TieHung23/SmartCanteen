using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Category.GetAllCategories;

public class GetAllCategoriesQuery : PaginationParams, IQuery<PaginatedList<GetAllCategoriesResponse>>
{
    public string? Name { get; set; }

    public GetAllCategoriesQuery()
    {
    }

    public GetAllCategoriesQuery(int pageNumber = 1, int pageSize = 10, string? name = null) 
        : base(pageNumber, pageSize)
    {
        Name = name;
    }
}
