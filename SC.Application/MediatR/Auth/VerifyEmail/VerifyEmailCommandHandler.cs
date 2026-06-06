using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Auth;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using UserAggregate = SC.Domain.Domain.User.User;
using EmailVerificationTokenAggregate = SC.Domain.Domain.User.EmailVerificationToken;

namespace SC.Application.MediatR.Auth.VerifyEmail;

internal class VerifyEmailCommandHandler(
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<EmailVerificationTokenAggregate, Guid> tokenRepository,
    IJwtTokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork,
    ILogger<VerifyEmailCommandHandler> logger) : ICommandHandler<VerifyEmailCommand, VerifyEmailResponse>
{
    public async Task<Result<VerifyEmailResponse>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tokenHash = tokenGenerator.HashOpaqueToken(request.Token);

            var token = await tokenRepository
                .GetQueryable(t => t.TokenHash == tokenHash)
                .FirstOrDefaultAsync(cancellationToken);

            if (token is null || !token.IsValid)
            {
                return Result.Failure<VerifyEmailResponse>(
                    Error.InvalidOrExpiredToken,
                    "The verification token is invalid or expired.");
            }

            var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<VerifyEmailResponse>(
                    Error.InvalidOrExpiredToken,
                    "Associated account no longer exists.");
            }

            user.ConfirmEmail();
            token.Consume();

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            userRepository.Update(user);
            tokenRepository.Update(token);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new VerifyEmailResponse
            {
                Message = "Email verified successfully."
            };

            return Result.Success(response, response.Message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error verifying email token");
            return Result.Failure<VerifyEmailResponse>(
                Error.ServerError,
                "An error occurred while verifying the email.");
        }
    }
}
