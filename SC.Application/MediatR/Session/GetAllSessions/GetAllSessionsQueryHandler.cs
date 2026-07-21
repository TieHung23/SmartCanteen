using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using CategoryAggregateRoot = SC.Domain.Domain.Category.AggregateRoot.Category;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Session.GetAllSessions;

internal class GetAllSessionsQueryHandler(
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<CategoryAggregateRoot, Guid> categoryRepository,
    ILogger<GetAllSessionsQueryHandler> logger
) : IQueryHandler<GetAllSessionsQuery, PaginatedList<GetAllSessionsResponse>>
{
    public async Task<Result<PaginatedList<GetAllSessionsResponse>>> Handle(
        GetAllSessionsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var allSessions = await sessionRepository.FindListAsync(
                null,
                q => q.Include(x => x.SessionDishes)
                      .Include(x => x.MealTemplates)
                      .ThenInclude(x => x.Settings),
                cancellationToken);

            var now = DateTimeOffset.UtcNow;
            var filtered = allSessions
                .Where(x => !x.IsDeleted)
                .AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                filtered = filtered.Where(x =>
                    x.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (request.IsActive.HasValue)
            {
                filtered = filtered.Where(x =>
                    SessionAvailability.IsOpenForOrder(x, now) == request.IsActive.Value);
            }

            var filteredList = filtered.ToList();
            var totalCount = filteredList.Count;

            var skipCount = request.GetSkipCount();
            var paginatedSessions = filteredList
                .OrderBy(x => x.Name)
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToList();

            var allDishIds = paginatedSessions
                .SelectMany(s => s.SessionDishes)
                .Select(d => d.DishId)
                .Distinct()
                .ToList();

            var dishes = await dishRepository
                .FindListAsync(d => allDishIds.Contains(d.Id), cancellationToken);

            var dishMap = dishes.ToDictionary(d => d.Id);

            var allCategoryIds = paginatedSessions
                .SelectMany(s => s.MealTemplates)
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

            var responses = paginatedSessions.Select(m => new GetAllSessionsResponse
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                IsActive = SessionAvailability.IsOpenForOrder(m, now),
                AvailableFrom = m.AvailableFrom,
                AvailableTo = m.AvailableTo,
                AvailableForOrder = m.AvailableForOrder,
                FinalizationDeadline = m.FinalizationDeadline,
                AutoFinalizePolicy = (int)m.AutoFinalizePolicy,
                IsFinalized = m.IsFinalized,
                FinalizedAtUtc = m.FinalizedAtUtc,
                CreatedAtUtc = m.CreatedAtUtc,
                UpdatedAtUtc = m.UpdatedAtUtc,
                CreatedBy = m.CreatedBy,
                MealTemplates = m.MealTemplates
                    .Where(template => !template.IsDeleted)
                    .Select(template => new MealTemplateDto
                    {
                        Id = template.Id,
                        Name = template.Name,
                        Settings = template.Settings
                            .Where(setting => !setting.IsDeleted)
                            .Select(setting => new MealSettingDto
                            {
                                Id = setting.Id,
                                MealTemplateId = setting.MealTemplateId,
                                CategoryId = setting.CategoryId,
                                CategoryName = categoryMap.GetValueOrDefault(setting.CategoryId)?.Name ?? string.Empty,
                                MinQuantity = setting.MinQuantity,
                                MaxQuantity = setting.MaxQuantity,
                                IsRequired = setting.IsRequired
                            }).ToList()
                    }).ToList(),
                Dishes = m.SessionDishes
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
            }).ToList();

            var paginatedResult = new PaginatedList<GetAllSessionsResponse>(
                responses,
                request.PageNumber,
                request.PageSize,
                totalCount);

            return Result.Success(paginatedResult, "Sessions retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving sessions");
            return Result.Failure<PaginatedList<GetAllSessionsResponse>>(
                Error.ServerError,
                "An error occurred while retrieving sessions.");
        }
    }
}
