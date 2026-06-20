namespace SC.Domain.Abstraction.Entities;

public abstract class Entity<T> : IEntity<T>
{
    public required T Id { get; init; }
}
