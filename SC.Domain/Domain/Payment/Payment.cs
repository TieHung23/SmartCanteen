using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.Payment;

public class Payment : Entity<Guid>, IAuditableEntity<Guid>
{
    public required BalanceSnapshot BalanceSnapshot { get; set; }
    
    public required string GatewayTransactionId { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentMethod Method { get; set; }
    
    public required User.User User { get; set; }
    public Guid UserId { get; set; }
    
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    
    private Payment()
    {
    }
    
    public static Payment Create(BalanceSnapshot balanceSnapshot, string gatewayTransactionId, PaymentMethod method, User.User user, Guid createdBy)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            BalanceSnapshot = balanceSnapshot,
            GatewayTransactionId = gatewayTransactionId,
            Status = PaymentStatus.Pending,
            Method = method,
            User = user,
            UserId = user.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }
    
    public void MarkAsPending()
    {
        Status = PaymentStatus.Pending;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
    
    public void MarkAsCompleted()
    {
        Status = PaymentStatus.Completed;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}