using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Auth.GetCurrentUser;

internal class GetCurrentUserQueryHandler(
    IGenericRepository<UserAggregate, Guid> userRepository,
    ICurrentUserService currentUserService,
    ILogger<GetCurrentUserQueryHandler> logger) : IQueryHandler<GetCurrentUserQuery, UserProfileResponse>
{
    public async Task<Result<UserProfileResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                return Result.Failure<UserProfileResponse>(Error.Forbidden, "Not authenticated.");
            }

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<UserProfileResponse>(Error.Forbidden, "User not found.");
            }

            var response = new UserProfileResponse(
                user.Id,
                user.Name,
                user.Email,
                user.ImgUrl,
                user.Role,
                user.Category,
                user.Status,
                user.EmailVerified,
                user.StudentId,
                user.DateOfBirth,
                user.MajorOrClass,
                user.PhoneNumber,
                user.Address,
                user.Gender,
                user.Balance.Amount,
                user.LastLoginAt);

            return Result.Success(response, "Profile retrieved.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving current user profile");
            return Result.Failure<UserProfileResponse>(Error.ServerError, "An error occurred while retrieving the profile.");
        }
    }
}
