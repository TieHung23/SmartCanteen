using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using CategoryAggregateRoot = SC.Domain.Domain.Category.AggregateRoot.Category;

namespace SC.Application.MediatR.Category.GetAllCategories;

internal class GetAllCategoriesQueryHandler(
    IGenericRepository<CategoryAggregateRoot, Guid> categoryRepository,
    ILogger<GetAllCategoriesQueryHandler> logger
) : IQueryHandler<GetAllCategoriesQuery, PaginatedList<GetAllCategoriesResponse>>
{
    public async Task<Result<PaginatedList<GetAllCategoriesResponse>>> Handle(
        GetAllCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            IQueryable<CategoryAggregateRoot> query = categoryRepository.GetQueryable();

            // Filter out deleted categories
            query = query.Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                query = query.Where(x =>
                    x.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var skipCount = request.GetSkipCount();
            var paginatedCategories = await query
                .OrderBy(x => x.Name)
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var responses = paginatedCategories.Select(c => new GetAllCategoriesResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            }).ToList();

            var paginatedResult = new PaginatedList<GetAllCategoriesResponse>(
                responses,
                request.PageNumber,
                request.PageSize,
                totalCount);

            return Result.Success(paginatedResult, "Categories retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving categories");
            return Result.Failure<PaginatedList<GetAllCategoriesResponse>>(
                Error.ServerError,
                "An error occurred while retrieving categories.");
        }
    }
}




