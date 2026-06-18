using System.Linq.Expressions;
using SC.Application.MediatR.Cart;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.SharedKernel.ValueObjects;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using MealAggregateRoot = SC.Domain.Domain.Meal.AggregateRoot.Meal;
using MealTemplateEntity = SC.Domain.Domain.Meal.Entity.MealTemplate;

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
    public async Task ValidateAsync_Should_Reject_Empty_Meal_List()
    {
        var result = await _service.ValidateAsync(new CartData());

        Assert.True(result.IsFailure);
        Assert.Equal("Cart must contain at least one meal.", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Empty_Items()
    {
        var result = await _service.ValidateAsync(new CartData
        {
            Meals =
            [
                new CartMealData
                {
                    MealId = Guid.NewGuid(),
                    MealTemplateId = Guid.NewGuid(),
                    Items = []
                }
            ]
        });

        Assert.True(result.IsFailure);
        Assert.Equal("Cart meal must contain at least one item.", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Invalid_Quantity()
    {
        var result = await _service.ValidateAsync(new CartData
        {
            Meals =
            [
                new CartMealData
                {
                    MealId = Guid.NewGuid(),
                    MealTemplateId = Guid.NewGuid(),
                    Items =
                    [
                        new CartItemData
                        {
                            DishId = Guid.NewGuid(),
                            Quantity = 0
                        }
                    ]
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
            Meals =
            [
                new CartMealData
                {
                    MealId = Guid.NewGuid(),
                    MealTemplateId = Guid.NewGuid(),
                    Items =
                    [
                        new CartItemData { DishId = dishId, Quantity = 1 },
                        new CartItemData { DishId = dishId, Quantity = 2 }
                    ]
                }
            ]
        });

        Assert.True(result.IsFailure);
        Assert.Equal("Cart cannot contain duplicate dishes in the same meal.", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Missing_MealId()
    {
        var result = await _service.ValidateAsync(new CartData
        {
            Meals =
            [
                new CartMealData
                {
                    Items =
                    [
                        new CartItemData
                        {
                            DishId = Guid.NewGuid(),
                            Quantity = 1
                        }
                    ]
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
            Meals =
            [
                new CartMealData
                {
                    MealId = Guid.NewGuid(),
                    MealTemplateId = Guid.NewGuid(),
                    Items = null
                }
            ]
        });

        Assert.True(result.IsFailure);
        Assert.Equal("Cart meal must contain at least one item.", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Missing_DishId()
    {
        var result = await _service.ValidateAsync(new CartData
        {
            Meals =
            [
                new CartMealData
                {
                    MealId = Guid.NewGuid(),
                    MealTemplateId = Guid.NewGuid(),
                    Items =
                    [
                        new CartItemData
                        {
                            DishId = Guid.Empty,
                            Quantity = 1
                        }
                    ]
                }
            ]
        });

        Assert.True(result.IsFailure);
        Assert.Equal("DishId is required for every cart item.", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Missing_MealTemplateId()
    {
        var result = await _service.ValidateAsync(new CartData
        {
            Meals =
            [
                new CartMealData
                {
                    MealId = Guid.NewGuid(),
                    Items =
                    [
                        new CartItemData
                        {
                            DishId = Guid.NewGuid(),
                            Quantity = 1
                        }
                    ]
                }
            ]
        });

        Assert.True(result.IsFailure);
        Assert.Equal("MealTemplateId is required.", result.Message);
    }

    [Fact]
    public void Cart_Should_Increment_Version_On_Every_Update()
    {
        var userId = Guid.NewGuid();
        var cart = SC.Domain.Domain.Cart.AggregateRoot.Cart.Create(
            userId,
            """{"mealId":"00000000-0000-0000-0000-000000000001","mealTemplateId":"00000000-0000-0000-0000-000000000003","items":[]}""");

        Assert.Equal(1, cart.Version);

        cart.Update(
            """{"mealId":"00000000-0000-0000-0000-000000000001","mealTemplateId":"00000000-0000-0000-0000-000000000003","items":[{"dishId":"00000000-0000-0000-0000-000000000002","quantity":1}]}""",
            userId);

        Assert.Equal(2, cart.Version);
        Assert.Equal(userId, cart.UpdatedBy);
        Assert.NotNull(cart.UpdatedAtUtc);
    }

    [Fact]
    public void TemplateRules_Should_Require_Required_Category()
    {
        var categoryId = Guid.NewGuid();
        var template = CreateTemplate(categoryId, 1, 2, true);

        var result = CartTemplateRuleValidator.Validate(
            template,
            [],
            new Dictionary<Guid, DishAggregateRoot>(),
            requireCompleteTemplate: true);

        Assert.True(result.IsFailure);
        Assert.Equal("The cart does not satisfy the required category quantities.", result.Message);
    }

    [Fact]
    public void TemplateRules_Should_Reject_Unconfigured_Category()
    {
        var configuredCategoryId = Guid.NewGuid();
        var otherCategoryId = Guid.NewGuid();
        var template = CreateTemplate(configuredCategoryId, 0, 2, false);
        var dish = CreateDish(otherCategoryId);
        var items = new[] { new CartItemData { DishId = dish.Id, Quantity = 1 } };

        var result = CartTemplateRuleValidator.Validate(
            template,
            items,
            new Dictionary<Guid, DishAggregateRoot> { [dish.Id] = dish },
            requireCompleteTemplate: true);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "One or more selected dish categories are not allowed by the meal template.",
            result.Message);
    }

    [Fact]
    public void TemplateRules_Should_Reject_Quantity_Above_Maximum()
    {
        var categoryId = Guid.NewGuid();
        var template = CreateTemplate(categoryId, 1, 2, true);
        var dish = CreateDish(categoryId);
        var items = new[] { new CartItemData { DishId = dish.Id, Quantity = 3 } };

        var result = CartTemplateRuleValidator.Validate(
            template,
            items,
            new Dictionary<Guid, DishAggregateRoot> { [dish.Id] = dish },
            requireCompleteTemplate: true);

        Assert.True(result.IsFailure);
        Assert.Equal("A selected category exceeds its maximum quantity.", result.Message);
    }

    [Fact]
    public void TemplateRules_Should_Accept_Valid_Selection()
    {
        var categoryId = Guid.NewGuid();
        var template = CreateTemplate(categoryId, 1, 2, true);
        var dish = CreateDish(categoryId);
        var items = new[] { new CartItemData { DishId = dish.Id, Quantity = 2 } };

        var result = CartTemplateRuleValidator.Validate(
            template,
            items,
            new Dictionary<Guid, DishAggregateRoot> { [dish.Id] = dish },
            requireCompleteTemplate: true);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void TemplateRules_Should_Reject_Duplicate_Category_Settings()
    {
        var categoryId = Guid.NewGuid();
        var template = CreateTemplate(categoryId, 1, 2, true);
        template.AddSetting(categoryId, 0, 3, false);

        var result = CartTemplateRuleValidator.Validate(
            template,
            [],
            new Dictionary<Guid, DishAggregateRoot>(),
            requireCompleteTemplate: true);

        Assert.True(result.IsFailure);
        Assert.Equal("Meal template contains duplicate category settings.", result.Message);
    }

    [Fact]
    public void TemplateRules_Should_Allow_Partial_Selection_When_Not_Checkout()
    {
        var categoryId = Guid.NewGuid();
        var template = CreateTemplate(categoryId, 2, 3, true);
        var dish = CreateDish(categoryId);
        var items = new[] { new CartItemData { DishId = dish.Id, Quantity = 1 } };

        var result = CartTemplateRuleValidator.Validate(
            template,
            items,
            new Dictionary<Guid, DishAggregateRoot> { [dish.Id] = dish },
            requireCompleteTemplate: false);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void TemplateRules_Should_Still_Reject_Maximum_In_Partial_Selection()
    {
        var categoryId = Guid.NewGuid();
        var template = CreateTemplate(categoryId, 0, 2, false);
        var dish = CreateDish(categoryId);
        var items = new[] { new CartItemData { DishId = dish.Id, Quantity = 3 } };

        var result = CartTemplateRuleValidator.Validate(
            template,
            items,
            new Dictionary<Guid, DishAggregateRoot> { [dish.Id] = dish },
            requireCompleteTemplate: false);

        Assert.True(result.IsFailure);
        Assert.Equal("A selected category exceeds its maximum quantity.", result.Message);
    }

    private static MealTemplateEntity CreateTemplate(
        Guid categoryId,
        int minQuantity,
        int maxQuantity,
        bool isRequired)
    {
        var template = MealTemplateEntity.Create(Guid.NewGuid(), "Test template");
        template.AddSetting(categoryId, minQuantity, maxQuantity, isRequired);
        return template;
    }

    private static DishAggregateRoot CreateDish(Guid categoryId)
    {
        return DishAggregateRoot.Create(
            "Test dish",
            "Test dish description",
            Money.Create(10),
            categoryId,
            Guid.NewGuid());
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
