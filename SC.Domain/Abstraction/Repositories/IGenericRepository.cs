using System.Linq.Expressions;
using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Abstraction.Repositories;

public interface IGenericRepository<TEntity, in TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties);

    IQueryable<TEntity> GetQueryable(Expression<Func<TEntity, bool>>? predicate = null,
        params Expression<Func<TEntity, object>>[] includeProperties);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Delete(TEntity entity);

    void DeleteRange(IEnumerable<TEntity> entities);
}
