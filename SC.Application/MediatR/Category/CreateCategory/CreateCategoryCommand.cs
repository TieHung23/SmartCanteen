using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Category.CreateCategory;

public class CreateCategoryCommand : ICommand<CreateCategoryResponse>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImgUrl { get; set; }
}
