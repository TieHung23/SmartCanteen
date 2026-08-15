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
            if (!UserAggregate.CanSelfRegister(request.Category))
            {
                return Result.Failure<RegisterUserResponse>(
                    Error.UnsupportedUserCategory,
                    "Registration is only available for Student and Lecturer accounts.");
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var emailTaken = await userRepository
                .ExistsAsync(u => u.Email == normalizedEmail, cancellationToken);

            if (emailTaken)
            {
                return Result.Failure<RegisterUserResponse>(
                    Error.EmailAlreadyExists,
                    "Email is already in use.");
            }

            if (!string.IsNullOrWhiteSpace(request.StudentId))
            {
                var studentIdTaken = await userRepository
                    .ExistsAsync(u => u.StudentId == request.StudentId, cancellationToken);

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

            var requiresVerificationCode = UserAggregate.RequiresEmailVerificationCode(request.Category);

            // Lecturer accounts skip the emailed code: the address is confirmed on creation,
            // which also settles the account status (Active for FPT mailboxes, otherwise
            // pending identity verification).
            if (!requiresVerificationCode)
            {
                user.ConfirmEmail();
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await userRepository.AddAsync(user, cancellationToken);

            VerificationCodeResult? verificationCode = null;

            if (requiresVerificationCode)
            {
                verificationCode = tokenGenerator.GenerateEmailVerificationCode();
                var ttl = TimeSpan.FromMinutes(configuration.GetValue("Jwt:EmailVerificationCodeMinutes", 5));
                var verificationToken = EmailVerificationTokenAggregate.Issue(user.Id, verificationCode.CodeHash, ttl);

                await tokenRepository.AddAsync(verificationToken, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            if (verificationCode is not null)
            {
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
            }

            var response = new RegisterUserResponse(user.Id, user.Category, requiresVerificationCode);

            return Result.Success(
                response,
                requiresVerificationCode
                    ? "Registration successful. Please verify your email."
                    : "Registration successful. You can sign in now.");
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
