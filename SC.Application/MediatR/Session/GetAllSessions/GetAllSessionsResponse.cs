namespace SC.Application.MediatR.Session.GetAllSessions;

public class GetAllSessionsResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset AvailableFrom { get; set; }
    public DateTimeOffset AvailableTo { get; set; }
    public DateTimeOffset AvailableForOrder { get; set; }
    public List<MealTemplateDto> MealTemplates { get; set; } = new();
    public List<SessionDishDto> Dishes { get; set; } = new();
}

public class MealTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<MealSettingDto> Settings { get; set; } = new();
}

public class MealSettingDto
{
    public Guid CategoryId { get; set; }
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
    public bool IsRequired { get; set; }
}

public class SessionDishDto
{
    public Guid DishId { get; set; }
    public int Quantity { get; set; }
}
