using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.Order.AggregateRoot;

public class Order : Entity<Guid>, IAuditableEntity<Guid>
{
    
    
    
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
}