using SC.Application.MediatR.Session.FinalizeSession;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Services;

namespace SC.Application.MediatR.Session.FinalizeSessionNow;

internal class FinalizeSessionNowCommandHandler(
    IFinalizeSessionService finalizeSessionService,
    ICurrentUserService currentUserService) : ICommandHandler<FinalizeSessionNowCommand, FinalizeSessionResponse>
{
    public async Task<Result<FinalizeSessionResponse>> Handle(FinalizeSessionNowCommand request, CancellationToken cancellationToken)
    {
        var preparedDishes = request.PreparedDishes
            .Select(d => (d.DishId, d.PreparedQuantity, d.SuggestedDishId))
            .ToList();

        var result = await finalizeSessionService.FinalizeAsync(
            request.SessionId,
            preparedDishes,
            currentUserService.UserId,
            cancellationToken,
            startServingNow: true);

        if (result.IsFailure)
            return Result.Failure<FinalizeSessionResponse>(result.Error, result.Message);

        var response = new FinalizeSessionResponse
        {
            Message = "Session finalized and serving started immediately."
        };

        return Result.Success(response, "Session finalized and serving started immediately.");
    }
}
