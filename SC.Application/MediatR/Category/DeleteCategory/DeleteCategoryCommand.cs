using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Category.DeleteCategory;

public class DeleteCategoryCommand : ICommand
{
    public Guid Id { get; set; }

    public DeleteCategoryCommand()
    {
    }

    public DeleteCategoryCommand(Guid id)
    {
        Id = id;
    }
}
