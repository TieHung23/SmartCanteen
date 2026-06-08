using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Auth.UpdateProfile;

internal class UpdateProfileCommandHandler(
    IGenericRepository<UserAggregate, Guid> userRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<UpdateProfileCommandHandler> logger)
    : ICommandHandler<UpdateProfileCommand, UpdateProfileResponse>
{
    public async Task<Result<UpdateProfileResponse>> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                return Result.Failure<UpdateProfileResponse>(
                    Error.Forbidden,
                    "Not authenticated.");
            }

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<UpdateProfileResponse>(
                    Error.Forbidden,
                    "User not found.");
            }

            var normalizedStudentId = NormalizeOptional(request.StudentId)?.ToUpperInvariant();
            if (normalizedStudentId is not null)
            {
                var studentIdTaken = await userRepository
                    .GetQueryable(candidate =>
                        candidate.Id != userId
                        && candidate.StudentId != null
                        && candidate.StudentId.ToUpper() == normalizedStudentId)
                    .AnyAsync(cancellationToken);

                if (studentIdTaken)
                {
                    return Result.Failure<UpdateProfileResponse>(
                        Error.StudentIdAlreadyUsed,
                        "Student ID is already linked to another account.");
                }
            }

            user.UpdateProfile(
                request.Name.Trim(),
                NormalizeOptional(request.ImgUrl),
                normalizedStudentId,
                request.DateOfBirth,
                NormalizeOptional(request.MajorOrClass),
                NormalizeOptional(request.PhoneNumber),
                NormalizeOptional(request.Address),
                request.Gender);

            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new UpdateProfileResponse(
                user.Id,
                user.Name,
                user.Email,
                user.ImgUrl,
                user.Role,
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

            return Result.Success(response, "Profile updated successfully.");
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is DbException { SqlState: "23505" } databaseException
                && databaseException.Message.Contains(
                    "IX_Users_StudentId",
                    StringComparison.Ordinal))
        {
            logger.LogWarning(
                ex,
                "Student ID conflict while updating profile for user {UserId}",
                currentUserService.UserId);
            return Result.Failure<UpdateProfileResponse>(
                Error.StudentIdAlreadyUsed,
                "Student ID is already linked to another account.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating profile for user {UserId}", currentUserService.UserId);
            return Result.Failure<UpdateProfileResponse>(
                Error.ServerError,
                "An error occurred while updating the profile.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
