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
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<EmailVerificationTokenAggregate, Guid> tokenRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator,
    IEmailSender emailSender,
    IConfiguration configuration,
    IUnitOfWork unitOfWork,
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
                .GetQueryable(u => u.Email == normalizedEmail)
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
                    .GetQueryable(u => u.StudentId == request.StudentId)
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
                studentId: string.IsNullOrWhiteSpace(request.StudentId) ? null : request.StudentId.Trim(),
                dateOfBirth: request.DateOfBirth,
                majorOrClass: string.IsNullOrWhiteSpace(request.MajorOrClass) ? null : request.MajorOrClass.Trim(),
                phoneNumber: string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                address: string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
                gender: request.Gender);

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await userRepository.AddAsync(user, cancellationToken);

            var verificationCode = tokenGenerator.GenerateEmailVerificationCode();
            var ttl = TimeSpan.FromMinutes(configuration.GetValue("Jwt:EmailVerificationCodeMinutes", 5));
            var verificationToken = EmailVerificationTokenAggregate.Issue(user.Id, verificationCode.CodeHash, ttl);

            await tokenRepository.AddAsync(verificationToken, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            try
            {
                await emailSender.SendVerificationCodeAsync(user.Email, verificationCode.Code, cancellationToken);
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
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error during user registration for {Email}", request.Email);
            return Result.Failure<RegisterUserResponse>(
                Error.ServerError,
                "An error occurred while creating the account.");
        }
    }
}
