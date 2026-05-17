using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Auth;
using SC.Contract.Services.Email;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using UserAggregate = SC.Domain.Domain.User.User;
using EmailVerificationTokenAggregate = SC.Domain.Domain.User.EmailVerificationToken;

namespace SC.Application.MediatR.Auth.Register;

internal class RegisterUserCommandHandler(
    IRepositoryBase<UserAggregate, Guid> userRepository,
    IRepositoryBase<EmailVerificationTokenAggregate, Guid> tokenRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<RegisterUserCommandHandler> logger) : ICommandHandler<RegisterUserCommand, RegisterUserResponse>
{
    public async Task<Result<RegisterUserResponse>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var emailTaken = await userRepository
                .FindAll(u => u!.Email == normalizedEmail, cancellationToken)
                .AnyAsync(cancellationToken);

            if (emailTaken)
            {
                return Result.Failure<RegisterUserResponse>(
                    Error.EmailAlreadyExists,
                    "Email is already in use.");
            }

            if (!string.IsNullOrWhiteSpace(request.StudentId))
            {
                var studentIdTaken = await userRepository
                    .FindAll(u => u!.StudentId == request.StudentId, cancellationToken)
                    .AnyAsync(cancellationToken);

                if (studentIdTaken)
                {
                    return Result.Failure<RegisterUserResponse>(
                        Error.StudentIdAlreadyUsed,
                        "Student ID is already linked to another account.");
                }
            }

            var passwordHash = passwordHasher.Hash(request.Password);

            var user = UserAggregate.Register(
                name: request.Name.Trim(),
                email: normalizedEmail,
                passwordHash: passwordHash,
                category: request.Category,
                studentId: string.IsNullOrWhiteSpace(request.StudentId) ? null : request.StudentId.Trim(),
                dateOfBirth: request.DateOfBirth,
                majorOrClass: string.IsNullOrWhiteSpace(request.MajorOrClass) ? null : request.MajorOrClass.Trim(),
                phoneNumber: string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                address: string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
                gender: request.Gender);

            var addUserResult = await userRepository.AddAsync(user);
            if (addUserResult.IsFailure)
            {
                return Result.Failure<RegisterUserResponse>(
                    addUserResult.Error ?? Error.ServerError,
                    addUserResult.Message);
            }

            var opaque = tokenGenerator.GenerateOpaqueToken();
            var ttl = TimeSpan.FromHours(configuration.GetValue("Jwt:EmailVerificationHours", 24));
            var verificationToken = EmailVerificationTokenAggregate.Issue(user.Id, opaque.TokenHash, ttl);

            var addTokenResult = await tokenRepository.AddAsync(verificationToken);
            if (addTokenResult.IsFailure)
            {
                return Result.Failure<RegisterUserResponse>(
                    addTokenResult.Error ?? Error.ServerError,
                    addTokenResult.Message);
            }

            try
            {
                await emailSender.SendVerificationLinkAsync(user.Email, opaque.RawToken, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to send verification email to {Email}. User and token are still persisted.",
                    user.Email);
            }

            return Result.Success(new RegisterUserResponse(user.Id), "Registration successful. Please verify your email.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during user registration for {Email}", request.Email);
            return Result.Failure<RegisterUserResponse>(
                Error.ServerError,
                "An error occurred while creating the account.");
        }
    }
}
