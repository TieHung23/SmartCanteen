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
    IRepositoryBase<UserAggregate, Guid> userRepository,
    IRepositoryBase<EmailVerificationTokenAggregate, Guid> tokenRepository,
    IJwtTokenGenerator tokenGenerator,
    ILogger<VerifyEmailCommandHandler> logger) : ICommandHandler<VerifyEmailCommand>
{
    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tokenHash = tokenGenerator.HashOpaqueToken(request.Token);

            var token = await tokenRepository
                .FindAll(t => t!.TokenHash == tokenHash, cancellationToken)
                .FirstOrDefaultAsync(cancellationToken);

            if (token is null || !token.IsValid)
            {
                return Result.Failure(Error.InvalidOrExpiredToken, "The verification token is invalid or expired.");
            }

            var user = await userRepository.FindByIdAsync(token.UserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure(Error.InvalidOrExpiredToken, "Associated account no longer exists.");
            }

            user.ConfirmEmail();
            token.Consume();

            var userUpdate = await userRepository.UpdateAsync(user);
            if (userUpdate.IsFailure)
                return Result.Failure(userUpdate.Error ?? Error.ServerError, userUpdate.Message);

            var tokenUpdate = await tokenRepository.UpdateAsync(token);
            if (tokenUpdate.IsFailure)
                return Result.Failure(tokenUpdate.Error ?? Error.ServerError, tokenUpdate.Message);

            return Result.Success("Email verified successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying email token");
            return Result.Failure(Error.ServerError, "An error occurred while verifying the email.");
        }
    }
}
