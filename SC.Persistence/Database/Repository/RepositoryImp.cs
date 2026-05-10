using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Repositories;

namespace SC.Persistence.Database.Repository;

public class RepositoryImp<TEntity, TKey>(SmartCanteenDbContext context, ILogger<RepositoryImp<TEntity, TKey>> logger) : IRepositoryBase<TEntity, TKey>
    where TEntity : class
    where TKey : AggregateRoot<Guid>
{
    private readonly SmartCanteenDbContext _context = context;
    private readonly ILogger<RepositoryImp<TEntity, TKey>> _logger = logger;

    public async Task<TEntity?> FindByIdAsync(TKey id, CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[]? includeProperties)
    {
        _logger.LogInformation("Finding entity {EntityType} by id {Id}", typeof(TEntity).Name, id.Id);

        var query = _context.Set<TEntity>().AsQueryable();

        if (includeProperties != null)
            query = includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));

        _logger.LogInformation("Executing SQL Script:\n{Sql}", query.ToQueryString());

        var result = await query.FirstOrDefaultAsync(e => e.GetType().GetProperty("Id")!.GetValue(e)!.Equals(id.Id), cancellationToken);

        return result!;
    }

    public Task<TEntity?> FindSingleAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[]? includeProperties)
    {
        _logger.LogInformation("Finding single entity {EntityType}", typeof(TEntity).Name);

        var query = _context.Set<TEntity>().AsQueryable();

        if (predicate != null) query = query.Where(predicate);

        if (includeProperties != null)
            query = includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));

        _logger.LogInformation("Executing SQL Script:\n{Sql}", query.ToQueryString());

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[]? includeProperties)
    {
        _logger.LogInformation("Finding all entities {EntityType}", typeof(TEntity).Name);

        var query = _context.Set<TEntity>().AsQueryable();

        if (predicate != null) query = query.Where(predicate);

        if (includeProperties != null)
            query = includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));

        _logger.LogInformation("Executing SQL Script:\n{Sql}", query.ToQueryString());

        return query;
    }
    public async Task<Result> AddAsync(TEntity entity)
    {
        try
        {
            _logger.LogInformation("Adding new entity {EntityType}", typeof(TEntity).Name);
            _context.Set<TEntity>().Add(entity);
            await _context.SaveChangesAsync();
            return Result.Success("Entity added successfully.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error adding entity {EntityType}", typeof(TEntity).Name);
            return Result.Failure(e is DbUpdateException ? Error.InvalidValue : Error.ServerError, e.Message);
        }
    }

    public async Task<Result> UpdateAsync(TEntity entity)
    {
        try
        {
            _logger.LogInformation("Updating entity {EntityType}", typeof(TEntity).Name);
            _context.Set<TEntity>().Update(entity);
            await _context.SaveChangesAsync();
            return Result.Success("Entity updated successfully.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating entity {EntityType}", typeof(TEntity).Name);
            return Result.Failure(e is DbUpdateException ? Error.InvalidValue : Error.ServerError, e.Message);
        }
    }

    public async Task<Result> DeleteAsync(TEntity entity)
    {
        try
        {
            _logger.LogInformation("Deleting entity {EntityType}", typeof(TEntity).Name);
            _context.Set<TEntity>().Remove(entity);
            await _context.SaveChangesAsync();
            return Result.Success("Entity deleted successfully.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deleting entity {EntityType}", typeof(TEntity).Name);
            return Result.Failure(Error.ServerError, e.Message);
        }
    }

    public async Task<Result> DeleteMultipleAsync(List<TEntity> entities)
    {
        try
        {
            _logger.LogInformation("Deleting multiple entities {EntityType}, count: {Count}", typeof(TEntity).Name, entities.Count);
            _context.Set<TEntity>().RemoveRange(entities);
            await _context.SaveChangesAsync();
            return Result.Success("Entities deleted successfully.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deleting multiple entities {EntityType}", typeof(TEntity).Name);
            return Result.Failure(Error.ServerError, e.Message);
        }
    }
}