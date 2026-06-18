namespace SC.Application.MediatR.Session.UpdateSession;

public class UpdateSessionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
