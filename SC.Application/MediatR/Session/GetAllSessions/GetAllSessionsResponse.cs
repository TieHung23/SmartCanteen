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
    public DateTimeOffset? FinalizationDeadline { get; set; }
    public int AutoFinalizePolicy { get; set; }
    public bool IsFinalized { get; set; }
    public DateTimeOffset? FinalizedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
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
    public Guid Id { get; set; }
    public Guid MealTemplateId { get; set; }
    public Guid CategoryId { get; set; }
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
    public bool IsRequired { get; set; }
}

public class SessionDishDto
{
    public Guid Id { get; set; }
    public Guid DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public string? ImgUrl { get; set; }
    public decimal PriceAmount { get; set; }
    public string PriceCurrency { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public int? PreparedQuantity { get; set; }
}
