using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Services;

namespace SC.Application.MediatR.Session.FinalizeSession;

internal class FinalizeSessionCommandHandler(
    IFinalizeSessionService finalizeSessionService,
    ICurrentUserService currentUserService) : ICommandHandler<FinalizeSessionCommand, FinalizeSessionResponse>
{
    public async Task<Result<FinalizeSessionResponse>> Handle(FinalizeSessionCommand request, CancellationToken cancellationToken)
    {
        var preparedDishes = request.PreparedDishes
            .Select(d => (d.DishId, d.PreparedQuantity, d.SuggestedDishId))
            .ToList();

        var result = await finalizeSessionService.FinalizeAsync(
            request.SessionId,
            preparedDishes,
            currentUserService.UserId,
            cancellationToken);

        if (result.IsFailure)
            return Result.Failure<FinalizeSessionResponse>(result.Error, result.Message);

        var response = new FinalizeSessionResponse
        {
            Message = "Session finalized successfully."
        };

        return Result.Success(response, "Session finalized successfully.");
    }
}
