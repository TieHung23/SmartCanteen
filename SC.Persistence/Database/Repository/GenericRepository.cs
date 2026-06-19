using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Abstraction.Repositories;

namespace SC.Persistence.Database.Repository;

public class GenericRepository<TEntity, TKey>(SmartCanteenDbContext context) : IGenericRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[]? includeProperties)
    {
        var query = _dbSet.AsQueryable();

        if (includeProperties is { Length: > 0 })
            query = includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));

        return await query.FirstOrDefaultAsync(e => e.Id!.Equals(id), cancellationToken);
    }

    public async Task<TEntity?> FindSingleAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[]? includeProperties)
    {
        var query = _dbSet.Where(predicate);

        if (includeProperties is { Length: > 0 })
            query = includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TEntity?> FindSingleAsync(Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> includeBuilder,
        CancellationToken cancellationToken = default)
    {
        var query = includeBuilder(_dbSet.Where(predicate));
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<TEntity>> FindListAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[]? includeProperties)
    {
        var query = predicate is null ? _dbSet.AsQueryable() : _dbSet.Where(predicate);

        if (includeProperties is { Length: > 0 })
            query = includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<TEntity>> FindListAsync(Expression<Func<TEntity, bool>>? predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> includeBuilder,
        CancellationToken cancellationToken = default)
    {
        var query = predicate is null ? _dbSet.AsQueryable() : _dbSet.Where(predicate);
        query = includeBuilder(query);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    public void DeleteRange(IEnumerable<TEntity> entities)
    {
        _dbSet.RemoveRange(entities);
    }
}
