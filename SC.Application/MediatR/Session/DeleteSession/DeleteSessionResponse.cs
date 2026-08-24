namespace SC.Application.MediatR.Session.DeleteSession;

public class DeleteSessionResponse
{
    public Guid Id { get; set; }

    /// <summary>
    /// Orders that were cancelled and refunded because the robot had not started serving them.
    /// </summary>
    public int RefundedOrderCount { get; set; }

    /// <summary>
    /// Orders left untouched: the robot is already assembling or has shelved them (the customer
    /// still collects those), or another full-order refund was already in flight for them.
    /// </summary>
    public int SkippedOrderCount { get; set; }

    public string Message { get; set; } = string.Empty;
}
