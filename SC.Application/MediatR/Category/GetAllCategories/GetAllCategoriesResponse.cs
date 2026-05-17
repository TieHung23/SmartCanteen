namespace SC.Application.MediatR.Category.GetAllCategories;

public class GetAllCategoriesResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}