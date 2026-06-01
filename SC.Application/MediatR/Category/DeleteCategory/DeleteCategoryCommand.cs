using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Category.DeleteCategory;

public record DeleteCategoryCommand(Guid Id) : ICommand<DeleteCategoryResponse>;
