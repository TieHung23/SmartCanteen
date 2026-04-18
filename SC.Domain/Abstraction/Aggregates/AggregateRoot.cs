using SC.Contract.Abstraction.Message;
using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Abstraction.Aggregates;

public abstract class AggregateRoot<T> : Entity<T>
{
    private readonly List<IDomainEvent> _domainEvents = [];
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}