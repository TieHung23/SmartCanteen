using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Dish.GetAllDishes;

public class GetAllDishesQuery : PaginationParams, IQuery<PaginatedList<GetAllDishesResponse>>
{
    public string? Name { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? MealId { get; set; }
    public bool? IsActive { get; set; }

    public GetAllDishesQuery()
    {
    }

    public GetAllDishesQuery(
        int pageNumber = 1,
        int pageSize = 10,
        string? name = null,
        Guid? categoryId = null,
        Guid? mealId = null,
        bool? isActive = null)
        : base(pageNumber, pageSize)
    {
        Name = name;
        CategoryId = categoryId;
        MealId = mealId;
        IsActive = isActive;
    }
}
