using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Session.GetAllSessions;

internal class GetAllSessionsQueryHandler(
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
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
                null, cancellationToken, x => x.SessionDishes, x => x.MealTemplates);

            var filtered = allSessions.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                filtered = filtered.Where(x =>
                    x.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (request.IsActive.HasValue)
            {
                filtered = filtered.Where(x => x.IsActive == request.IsActive.Value);
            }

            var filteredList = filtered.ToList();
            var totalCount = filteredList.Count;

            var skipCount = request.GetSkipCount();
            var paginatedSessions = filteredList
                .OrderBy(x => x.Name)
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToList();

            var responses = paginatedSessions.Select(m => new GetAllSessionsResponse
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                IsActive = m.IsActive,
                AvailableFrom = m.AvailableFrom,
                AvailableTo = m.AvailableTo,
                AvailableForOrder = m.AvailableForOrder,
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
                                CategoryId = setting.CategoryId,
                                MinQuantity = setting.MinQuantity,
                                MaxQuantity = setting.MaxQuantity,
                                IsRequired = setting.IsRequired
                            }).ToList()
                    }).ToList(),
                Dishes = m.SessionDishes
                    .Select(dm => new SessionDishDto
                    {
                        DishId = dm.DishId,
                        Quantity = dm.Quantity
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
