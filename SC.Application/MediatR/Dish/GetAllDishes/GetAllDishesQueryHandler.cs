using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Dish.GetAllDishes;

internal class GetAllDishesQueryHandler(
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    ILogger<GetAllDishesQueryHandler> logger
) : IQueryHandler<GetAllDishesQuery, PaginatedList<GetAllDishesResponse>>
{
    public async Task<Result<PaginatedList<GetAllDishesResponse>>> Handle(
        GetAllDishesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var allDishes = await dishRepository
                .FindListAsync(x => !x.IsDeleted, cancellationToken);
            var filtered = allDishes.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var normalizedName = request.Name.ToLower();
                filtered = filtered.Where(x => x.Name.ToLower().Contains(normalizedName));
            }

            if (request.CategoryId.HasValue)
            {
                filtered = filtered.Where(x => x.CategoryId == request.CategoryId.Value);
            }

            if (request.IsActive.HasValue)
            {
                filtered = filtered.Where(x => x.IsActive == request.IsActive.Value);
            }

            var filteredList = filtered.ToList();
            var totalCount = filteredList.Count;

            var skipCount = request.GetSkipCount();
            var paginatedDishes = filteredList
                .OrderBy(x => x.Name)
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToList();

            var responses = paginatedDishes.Select(d => new GetAllDishesResponse
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                Price = d.Price.Amount,
                IsActive = d.IsActive,
                CategoryId = d.CategoryId,
                ImgUrl = d.ImgUrl
            }).ToList();

            var paginatedResult = new PaginatedList<GetAllDishesResponse>(
                responses,
                request.PageNumber,
                request.PageSize,
                totalCount);

            return Result.Success(paginatedResult, "Dishes retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving dishes");
            return Result.Failure<PaginatedList<GetAllDishesResponse>>(
                Error.ServerError,
                "An error occurred while retrieving dishes.");
        }
    }
}
