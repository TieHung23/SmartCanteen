using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Abstraction.Repositories;

namespace SC.Persistence.Database.Repository;

public class RepositoryImp<TEntity, TKey>(SmartCanteenDbContext context, ILogger<RepositoryImp<TEntity, TKey>> logger) : IRepositoryBase<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    private readonly SmartCanteenDbContext _context = context;
    private readonly ILogger<RepositoryImp<TEntity, TKey>> _logger = logger;

    public async Task<TEntity?> FindByIdAsync(TKey id, CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[]? includeProperties)
    {
        _logger.LogInformation("Finding entity {EntityType} by id {Id}", typeof(TEntity).Name, id);

        var query = _context.Set<TEntity>().AsQueryable();

        if (includeProperties != null)
            query = includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));

        _logger.LogInformation("Executing SQL Script:\n{Sql}", query.ToQueryString());

        var result = await query.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);

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

    /// <summary>
    /// Update entities matching a condition (Aggregate Root only)
    /// </summary>
    /// <param name="predicate">Condition to match entities</param>
    /// <param name="updateAction">Action to update matched entities</param>
    /// <returns>Result with count of updated entities</returns>
    public async Task<Result> UpdateWithConditionAsync(
        Expression<Func<TEntity, bool>> predicate,
        Action<TEntity> updateAction)
    {
        try
        {
            _logger.LogInformation("Updating entities {EntityType} with condition", typeof(TEntity).Name);

            var entitiesToUpdate = await _context.Set<TEntity>()
                .Where(predicate)
                .ToListAsync();

            if (!entitiesToUpdate.Any())
            {
                _logger.LogInformation("No entities found matching the condition for {EntityType}", typeof(TEntity).Name);
                return Result.Success("No entities matched the condition.");
            }

            foreach (var entity in entitiesToUpdate)
            {
                updateAction(entity);
            }

            _logger.LogInformation("Updated {Count} entities {EntityType}", entitiesToUpdate.Count, typeof(TEntity).Name);
            await _context.SaveChangesAsync();

            return Result.Success($"{entitiesToUpdate.Count} entities updated successfully.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating entities {EntityType} with condition", typeof(TEntity).Name);
            return Result.Failure(e is DbUpdateException ? Error.InvalidValue : Error.ServerError, e.Message);
        }
    }

    /// <summary>
    /// Soft delete entities matching a condition using a boolean flag (Aggregate Root only)
    /// </summary>
    /// <param name="predicate">Condition to match entities</param>
    /// <param name="flagProperty">Property name for soft delete flag (e.g., "IsDeleted")</param>
    /// <returns>Result with count of soft deleted entities</returns>
    public async Task<Result> SoftDeleteWithConditionAsync(
        Expression<Func<TEntity, bool>> predicate,
        string flagProperty = "IsDeleted")
    {
        try
        {
            _logger.LogInformation("Soft deleting entities {EntityType} with condition using flag {Flag}",
                typeof(TEntity).Name, flagProperty);

            var entitiesToDelete = await _context.Set<TEntity>()
                .Where(predicate)
                .ToListAsync();

            if (!entitiesToDelete.Any())
            {
                _logger.LogInformation("No entities found matching the soft delete condition for {EntityType}",
                    typeof(TEntity).Name);
                return Result.Success("No entities matched the condition.");
            }

            foreach (var entity in entitiesToDelete)
            {
                var property = typeof(TEntity).GetProperty(flagProperty);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(entity, true);
                }
                else
                {
                    _logger.LogWarning("Property {Property} not found or not writable on {EntityType}",
                        flagProperty, typeof(TEntity).Name);
                }
            }

            _logger.LogInformation("Soft deleted {Count} entities {EntityType}",
                entitiesToDelete.Count, typeof(TEntity).Name);
            await _context.SaveChangesAsync();

            return Result.Success($"{entitiesToDelete.Count} entities soft deleted successfully.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error soft deleting entities {EntityType} with condition", typeof(TEntity).Name);
            return Result.Failure(e is DbUpdateException ? Error.InvalidValue : Error.ServerError, e.Message);
        }
    }

    /// <summary>
    /// Bulk update matching entities with a specific property value (Aggregate Root only)
    /// </summary>
    /// <param name="predicate">Condition to match entities</param>
    /// <param name="propertyName">Property name to update</param>
    /// <param name="newValue">New value to set</param>
    /// <returns>Result with count of updated entities</returns>
    public async Task<Result> BulkUpdatePropertyAsync(
        Expression<Func<TEntity, bool>> predicate,
        string propertyName,
        object? newValue)
    {
        try
        {
            _logger.LogInformation("Bulk updating property {Property} on entities {EntityType} with condition",
                propertyName, typeof(TEntity).Name);

            var entitiesToUpdate = await _context.Set<TEntity>()
                .Where(predicate)
                .ToListAsync();

            if (!entitiesToUpdate.Any())
            {
                _logger.LogInformation("No entities found for bulk update on {EntityType}", typeof(TEntity).Name);
                return Result.Success("No entities matched the condition.");
            }

            var property = typeof(TEntity).GetProperty(propertyName);
            if (property == null || !property.CanWrite)
            {
                _logger.LogError("Property {Property} not found or not writable on {EntityType}",
                    propertyName, typeof(TEntity).Name);
                return Result.Failure(Error.InvalidValue,
                    $"Property '{propertyName}' not found or not writable.");
            }

            foreach (var entity in entitiesToUpdate)
            {
                property.SetValue(entity, newValue);
            }

            _logger.LogInformation("Bulk updated {Count} entities {EntityType}, property {Property}",
                entitiesToUpdate.Count, typeof(TEntity).Name, propertyName);
            await _context.SaveChangesAsync();

            return Result.Success($"{entitiesToUpdate.Count} entities updated successfully.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error bulk updating property {Property} on {EntityType}",
                propertyName, typeof(TEntity).Name);
            return Result.Failure(e is DbUpdateException ? Error.InvalidValue : Error.ServerError, e.Message);
        }
    }
}