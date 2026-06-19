using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Session.GetSessionById;

internal class GetSessionByIdQueryHandler(
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
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
                .FindSingleAsync(x => x.Id == request.Id, cancellationToken,
                    x => x.MealTemplates, x => x.SessionDishes);

            if (session is null)
            {
                return Result.Failure<GetSessionByIdResponse>(
                    Error.NullValue,
                    $"Session with id {request.Id} not found.");
            }

            var response = new GetSessionByIdResponse
            {
                Id = session.Id,
                Name = session.Name,
                Description = session.Description,
                IsActive = session.IsActive,
                AvailableFrom = session.AvailableFrom,
                AvailableTo = session.AvailableTo,
                AvailableForOrder = session.AvailableForOrder,
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
                                CategoryId = s.CategoryId,
                                MinQuantity = s.MinQuantity,
                                MaxQuantity = s.MaxQuantity,
                                IsRequired = s.IsRequired
                            }).ToList()
                    })
                    .ToList(),
                Dishes = session.SessionDishes
                    .Select(dm => new SessionDishDto
                    {
                        DishId = dm.DishId,
                        Quantity = dm.Quantity
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
