using SC.Contract.Abstraction.Message;
using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Abstraction.Aggregates;


/// <summary>
/// Root Domain Object that contains a cluster of related objects. It is the main entry point for accessing and modifying the state of the aggregate. It is responsible for enforcing the invariants of the aggregate and ensuring that all changes to the aggregate are consistent with its rules and constraints.
/// </summary>
/// <typeparam name="T"></typeparam>
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