using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Session.CreateSession;

public class CreateSessionCommand : ICommand<CreateSessionResponse>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset AvailableFrom { get; set; }
    public DateTimeOffset AvailableTo { get; set; }
    public DateTimeOffset AvailableForOrder { get; set; }
    public List<MealTemplateInput> MealTemplates { get; set; } = new();
    public List<SessionDishInput> Dishes { get; set; } = new();
}

public class MealTemplateInput
{
    public string Name { get; set; } = string.Empty;
    public List<MealSettingInput> Settings { get; set; } = new();
}

public class MealSettingInput
{
    public Guid CategoryId { get; set; }
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
    public bool IsRequired { get; set; }
}

public class SessionDishInput
{
    public Guid DishId { get; set; }
    public int Quantity { get; set; } = 1;
}
