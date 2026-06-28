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
            var allCategories = await categoryRepository
                .FindListAsync(x => !x.IsDeleted, cancellationToken);
            var filtered = allCategories.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                filtered = filtered.Where(x =>
                    x.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase));
            }

            var filteredList = filtered.ToList();
            var totalCount = filteredList.Count;

            var skipCount = request.GetSkipCount();
            var paginatedCategories = filteredList
                .OrderBy(x => x.Name)
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToList();

            var responses = paginatedCategories.Select(c => new GetAllCategoriesResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description ?? string.Empty,
                ImgUrl = c.ImgUrl
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




