using System.Linq.Expressions;
using SC.Contract.Shared;

namespace SC.Domain.Abstraction.Repositories;

public interface IRepositoryBase<TEntity, in TKey>
    where TEntity : class
{
    Task<TEntity?> FindByIdAsync(TKey id, CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties);

    Task<TEntity?> FindSingleAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includeProperties);

    IQueryable<TEntity?> FindAll(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includeProperties);

    Task<Result> AddAsync(TEntity entity);
    Task<Result> UpdateAsync(TEntity entity);
    Task<Result> DeleteAsync(TEntity entity);
    Task<Result> DeleteMultipleAsync(List<TEntity> entities);

    /// <summary>
    /// Update entities matching a condition (Aggregate Root only)
    /// </summary>
    Task<Result> UpdateWithConditionAsync(
        Expression<Func<TEntity, bool>> predicate,
        Action<TEntity> updateAction);

    /// <summary>
    /// Soft delete entities matching a condition using a boolean flag (Aggregate Root only)
    /// </summary>
    Task<Result> SoftDeleteWithConditionAsync(
        Expression<Func<TEntity, bool>> predicate,
        string flagProperty = "IsDeleted");

    /// <summary>
    /// Bulk update matching entities with a specific property value (Aggregate Root only)
    /// </summary>
    Task<Result> BulkUpdatePropertyAsync(
        Expression<Func<TEntity, bool>> predicate,
        string propertyName,
        object? newValue);
}