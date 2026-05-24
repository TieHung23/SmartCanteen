namespace SC.Application.MediatR.Meal.CreateMeal;

public class CreateMealResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
