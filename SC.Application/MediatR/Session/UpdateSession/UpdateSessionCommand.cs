using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Session.UpdateSession;

public class UpdateSessionCommand : ICommand<UpdateSessionResponse>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset AvailableFrom { get; set; }
    public DateTimeOffset AvailableTo { get; set; }
    public DateTimeOffset AvailableForOrder { get; set; }
    public List<MealTemplateUpdate> MealTemplates { get; set; } = new();
    public List<SessionDishUpdate> Dishes { get; set; } = new();
}

public class MealTemplateUpdate
{
    public string Name { get; set; } = string.Empty;
    public List<MealSettingUpdate> Settings { get; set; } = new();
}

public class MealSettingUpdate
{
    public Guid CategoryId { get; set; }
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
    public bool IsRequired { get; set; }
}

public class SessionDishUpdate
{
    public Guid DishId { get; set; }
}
