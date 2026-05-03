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
}