using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Abstraction.Repositories;

namespace SC.Persistence.Database.Repository;

public class GenericRepository<TEntity, TKey>(SmartCanteenDbContext context, ILogger<GenericRepository<TEntity, TKey>> logger) : IGenericRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    private readonly SmartCanteenDbContext _context = context;
    private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();
    private readonly ILogger<GenericRepository<TEntity, TKey>> _logger = logger;

    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[]? includeProperties)
    {
        _logger.LogInformation("Getting entity {EntityType} by id {Id}", typeof(TEntity).Name, id);

        var query = _dbSet.AsQueryable();

        if (includeProperties is not null)
            query = includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));

        return await query.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public IQueryable<TEntity> GetQueryable(Expression<Func<TEntity, bool>>? predicate = null,
        params Expression<Func<TEntity, object>>[]? includeProperties)
    {
        _logger.LogInformation("Getting queryable for entity {EntityType}", typeof(TEntity).Name);

        var query = _dbSet.AsQueryable();

        if (predicate is not null)
            query = query.Where(predicate);

        if (includeProperties is not null)
            query = includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));

        return query;
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding entity {EntityType}", typeof(TEntity).Name);
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        _logger.LogInformation("Updating entity {EntityType}", typeof(TEntity).Name);
        _dbSet.Update(entity);
    }

    public void Delete(TEntity entity)
    {
        _logger.LogInformation("Deleting entity {EntityType}", typeof(TEntity).Name);
        _dbSet.Remove(entity);
    }

    public void DeleteRange(IEnumerable<TEntity> entities)
    {
        _logger.LogInformation("Deleting {Count} entities {EntityType}", entities.Count(), typeof(TEntity).Name);
        _dbSet.RemoveRange(entities);
    }
}
