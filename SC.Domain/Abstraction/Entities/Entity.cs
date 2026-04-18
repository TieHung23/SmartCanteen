using System.ComponentModel.DataAnnotations;

namespace SC.Domain.Abstraction.Entities;

public class Entity<T> : IEntity<T>
{
    [Key]
    public T Id { get; protected set; }
    
    public bool IsDeleted { get; protected set; }
}