namespace SC.Application.MediatR.Session.CreateSession;

public class CreateSessionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
