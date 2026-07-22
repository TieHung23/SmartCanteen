using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using CategoryAggregateRoot = SC.Domain.Domain.Category.AggregateRoot.Category;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Session.GetSessionById;

internal class GetSessionByIdQueryHandler(
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<CategoryAggregateRoot, Guid> categoryRepository,
    ILogger<GetSessionByIdQueryHandler> logger
) : IQueryHandler<GetSessionByIdQuery, GetSessionByIdResponse>
{
    public async Task<Result<GetSessionByIdResponse>> Handle(
        GetSessionByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await sessionRepository
                .FindSingleAsync(x => x.Id == request.Id,
                    q => q.Include(x => x.MealTemplates)
                          .ThenInclude(x => x.Settings)
                          .Include(x => x.SessionDishes),
                    cancellationToken);

            if (session is null)
            {
                return Result.Failure<GetSessionByIdResponse>(
                    Error.NullValue,
                    $"Session with id {request.Id} not found.");
            }

            var dishIds = session.SessionDishes.Select(d => d.DishId).Distinct().ToList();
            var dishes = await dishRepository
                .FindListAsync(d => dishIds.Contains(d.Id) && d.IsDeleted == false, cancellationToken);
            var dishMap = dishes.ToDictionary(d => d.Id);

            var allCategoryIds = session.MealTemplates
                .SelectMany(t => t.Settings)
                .Select(s => s.CategoryId)
                .Concat(dishes.Select(d => d.CategoryId))
                .Distinct()
                .ToList();

            var categories = allCategoryIds.Count > 0
                ? await categoryRepository.FindListAsync(
                    c => allCategoryIds.Contains(c.Id), cancellationToken)
                : [];

            var categoryMap = categories.ToDictionary(c => c.Id);

            var response = new GetSessionByIdResponse
            {
                Id = session.Id,
                Name = session.Name,
                Description = session.Description,
                IsActive = session.IsActive,
                AvailableFrom = session.AvailableFrom,
                AvailableTo = session.AvailableTo,
                AvailableForOrder = session.AvailableForOrder,
                FinalizationDeadline = session.FinalizationDeadline,
                AutoFinalizePolicy = (int)session.AutoFinalizePolicy,
                IsFinalized = session.IsFinalized,
                FinalizedAtUtc = session.FinalizedAtUtc,
                CreatedAtUtc = session.CreatedAtUtc,
                UpdatedAtUtc = session.UpdatedAtUtc,
                CreatedBy = session.CreatedBy,
                MealTemplates = session.MealTemplates
                    .Where(t => !t.IsDeleted)
                    .Select(t => new MealTemplateDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Settings = t.Settings
                            .Where(s => !s.IsDeleted)
                            .Select(s => new MealSettingDto
                            {
                                Id = s.Id,
                                MealTemplateId = s.MealTemplateId,
                                CategoryId = s.CategoryId,
                                CategoryName = categoryMap.GetValueOrDefault(s.CategoryId)?.Name ?? string.Empty,
                                MinQuantity = s.MinQuantity,
                                MaxQuantity = s.MaxQuantity,
                                IsRequired = s.IsRequired
                            }).ToList()
                    })
                    .ToList(),
                Dishes = session.SessionDishes
                    .Where(dm => dishMap.ContainsKey(dm.DishId))
                    .Select(dm =>
                    {
                        var dish = dishMap.GetValueOrDefault(dm.DishId);
                        return new SessionDishDto
                        {
                            Id = dm.Id,
                            DishId = dm.DishId,
                            DishName = dish?.Name ?? string.Empty,
                            ImgUrl = dish?.ImgUrl,
                            PriceAmount = dish?.Price.Amount ?? 0,
                            PriceCurrency = dish?.Price.Currency ?? string.Empty,
                            CategoryId = dish?.CategoryId ?? Guid.Empty,
                            CategoryName = dish is not null
                                ? categoryMap.GetValueOrDefault(dish.CategoryId)?.Name ?? string.Empty
                                : string.Empty,
                            PreparedQuantity = dm.PreparedQuantity
                        };
                    })
                    .ToList()
            };

            return Result.Success(response, "Session retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving session with id {SessionId}", request.Id);
            return Result.Failure<GetSessionByIdResponse>(
                Error.ServerError,
                "An error occurred while retrieving the session.");
        }
    }
}
