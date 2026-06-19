using Microsoft.EntityFrameworkCore;
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
            IQueryable<SessionAggregateRoot> query = sessionRepository.GetQueryable();

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                query = query.Where(x =>
                    x.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == request.IsActive.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var skipCount = request.GetSkipCount();
            var paginatedSessions = await query
                .Include(x => x.SessionDishes)
                .Include(x => x.MealTemplates)
                    .ThenInclude(x => x.Settings)
                .OrderBy(x => x.Name)
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

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
