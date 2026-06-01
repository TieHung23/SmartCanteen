namespace SC.Application.MediatR.Verification.Admin.Reject;

public class RejectVerificationResponse
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
}
