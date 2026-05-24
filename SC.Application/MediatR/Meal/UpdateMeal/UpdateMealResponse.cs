namespace SC.Application.MediatR.Meal.UpdateMeal;

public class UpdateMealResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
