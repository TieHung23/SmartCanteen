using System.ComponentModel.DataAnnotations;

namespace SC.Domain.Abstraction.Entities;

public abstract class Entity<T> : IEntity<T>
{
    [Key] public required T Id { get; init; }

    public bool IsDeleted { get; protected set; }

    public virtual void SoftDelete()
    {
        IsDeleted = true;
    }
}