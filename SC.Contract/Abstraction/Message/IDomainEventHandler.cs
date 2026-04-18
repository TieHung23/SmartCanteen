using MediatR;

namespace SC.Contract.Abstraction.Message;

public interface IDomainEventHandler : INotificationHandler<IDomainEvent>
{
    
}