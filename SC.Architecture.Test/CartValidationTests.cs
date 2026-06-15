using System.Linq.Expressions;
using SC.Application.MediatR.Cart;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Abstraction.Repositories;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using MealAggregateRoot = SC.Domain.Domain.Meal.AggregateRoot.Meal;

namespace SC.Architecture.Test;

public class CartValidationTests
{
    private readonly CartValidationService _service = new(
        new UnusedRepository<MealAggregateRoot>(),
        new UnusedRepository<DishAggregateRoot>());

    [Fact]
    public async Task ValidateAsync_Should_Reject_Null_Data()
    {
        var result = await _service.ValidateAsync(null);

        Assert.True(result.IsFailure);
        Assert.Equal("Cart data is required.", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Empty_Items()
    {
        var result = await _service.ValidateAsync(new CartData
        {
            MealId = Guid.NewGuid(),
            Items = []
        });

        Assert.True(result.IsFailure);
        Assert.Equal("Cart must contain at least one item.", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Invalid_Quantity()
    {
        var result = await _service.ValidateAsync(new CartData
        {
            MealId = Guid.NewGuid(),
            Items =
            [
                new CartItemData
                {
                    DishId = Guid.NewGuid(),
                    Quantity = 0
                }
            ]
        });

        Assert.True(result.IsFailure);
        Assert.Equal("Item quantity must be greater than zero.", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Duplicate_Dishes()
    {
        var dishId = Guid.NewGuid();
        var result = await _service.ValidateAsync(new CartData
        {
            MealId = Guid.NewGuid(),
            Items =
            [
                new CartItemData { DishId = dishId, Quantity = 1 },
                new CartItemData { DishId = dishId, Quantity = 2 }
            ]
        });

        Assert.True(result.IsFailure);
        Assert.Equal("Cart cannot contain duplicate dishes.", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Missing_MealId()
    {
        var result = await _service.ValidateAsync(new CartData
        {
            Items =
            [
                new CartItemData
                {
                    DishId = Guid.NewGuid(),
                    Quantity = 1
                }
            ]
        });

        Assert.True(result.IsFailure);
        Assert.Equal("MealId is required.", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Null_Items()
    {
        var result = await _service.ValidateAsync(new CartData
        {
            MealId = Guid.NewGuid(),
            Items = null
        });

        Assert.True(result.IsFailure);
        Assert.Equal("Cart must contain at least one item.", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Missing_DishId()
    {
        var result = await _service.ValidateAsync(new CartData
        {
            MealId = Guid.NewGuid(),
            Items =
            [
                new CartItemData
                {
                    DishId = Guid.Empty,
                    Quantity = 1
                }
            ]
        });

        Assert.True(result.IsFailure);
        Assert.Equal("DishId is required for every cart item.", result.Message);
    }

    [Fact]
    public void Cart_Should_Increment_Version_On_Every_Update()
    {
        var userId = Guid.NewGuid();
        var cart = SC.Domain.Domain.Cart.AggregateRoot.Cart.Create(
            userId,
            """{"mealId":"00000000-0000-0000-0000-000000000001","items":[]}""");

        Assert.Equal(1, cart.Version);

        cart.Update(
            """{"mealId":"00000000-0000-0000-0000-000000000001","items":[{"dishId":"00000000-0000-0000-0000-000000000002","quantity":1}]}""",
            userId);

        Assert.Equal(2, cart.Version);
        Assert.Equal(userId, cart.UpdatedBy);
        Assert.NotNull(cart.UpdatedAtUtc);
    }

    private sealed class UnusedRepository<TEntity> : IGenericRepository<TEntity, Guid>
        where TEntity : Entity<Guid>
    {
        public Task<TEntity?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includeProperties)
        {
            throw new InvalidOperationException("Repository should not be used by these validation tests.");
        }

        public IQueryable<TEntity> GetQueryable(
            Expression<Func<TEntity, bool>>? predicate = null,
            params Expression<Func<TEntity, object>>[] includeProperties)
        {
            throw new InvalidOperationException("Repository should not be used by these validation tests.");
        }

        public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public void Update(TEntity entity)
        {
            throw new NotSupportedException();
        }

        public void Delete(TEntity entity)
        {
            throw new NotSupportedException();
        }

        public void DeleteRange(IEnumerable<TEntity> entities)
        {
            throw new NotSupportedException();
        }
    }
}
