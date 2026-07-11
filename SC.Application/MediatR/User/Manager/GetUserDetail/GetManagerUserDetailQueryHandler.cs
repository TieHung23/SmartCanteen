using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.User.Manager.GetUserDetail;

internal class GetManagerUserDetailQueryHandler(
    IGenericRepository<UserAggregate, Guid> userRepository,
    ILogger<GetManagerUserDetailQueryHandler> logger)
    : IQueryHandler<GetManagerUserDetailQuery, ManagerUserDetailResponse>
{
    public async Task<Result<ManagerUserDetailResponse>> Handle(
        GetManagerUserDetailQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null || user.IsDeleted)
            {
                return Result.Failure<ManagerUserDetailResponse>(
                    Error.UserNotFound,
                    "User not found.");
            }

            var response = new ManagerUserDetailResponse(
                user.Id,
                user.Name,
                user.Email,
                user.ImgUrl,
                user.Role,
                user.Status,
                user.StatusReason,
                user.EmailVerified,
                user.StudentId,
                user.DateOfBirth,
                user.MajorOrClass,
                user.PhoneNumber,
                user.Address,
                user.Gender,
                user.Balance.Amount,
                user.LastLoginAt,
                user.CreatedAtUtc,
                user.UpdatedAtUtc);

            return Result.Success(response, "User detail retrieved.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user detail {UserId}", request.UserId);
            return Result.Failure<ManagerUserDetailResponse>(
                Error.ServerError,
                "An error occurred while retrieving user detail.");
        }
    }
}
