namespace SC.Application.MediatR.Category.UpdateCategory;

public class UpdateCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImgUrl { get; set; }
}
