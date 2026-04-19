using System.ComponentModel.DataAnnotations;

namespace SC.Domain.Abstraction.Entities;

public class Entity<T> : IEntity<T>
{
    [Key]
    public required T Id { get; init; }
    
    public bool IsDeleted { get; protected set; }
}