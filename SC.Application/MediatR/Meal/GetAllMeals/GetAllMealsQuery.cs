using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Meal.GetAllMeals;

public class GetAllMealsQuery : PaginationParams, IQuery<PaginatedList<GetAllMealsResponse>>
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }

    public GetAllMealsQuery()
    {
    }

    public GetAllMealsQuery(int pageNumber = 1, int pageSize = 10, string? name = null, bool? isActive = null)
        : base(pageNumber, pageSize)
    {
        Name = name;
        IsActive = isActive;
    }
}
