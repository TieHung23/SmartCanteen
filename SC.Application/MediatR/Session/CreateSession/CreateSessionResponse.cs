namespace SC.Application.MediatR.Session.CreateSession;

public class CreateSessionResponse
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
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public string Message { get; set; } = string.Empty;
}
