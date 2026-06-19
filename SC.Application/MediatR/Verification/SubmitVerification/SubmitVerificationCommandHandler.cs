using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Storage;
using SC.Contract.Services.Verification;
using SC.Contract.Shared;
using SC.Contract.Services.Notification;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.User.Enum;
using SC.Domain.Domain.Verification.AggregateRoot;
using SC.Domain.Domain.Verification.Enum;
using SC.Domain.Domain.Verification.ValueObject;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Verification.SubmitVerification;

internal class SubmitVerificationCommandHandler(
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<VerificationRequest, Guid> verificationRepository,
    IFileValidator fileValidator,
    IFileUploader fileUploader,
    ICurrentUserService currentUserService,
    IConfiguration configuration,
    IUnitOfWork unitOfWork,
    IBusinessNotificationService businessNotificationService,
    ILogger<SubmitVerificationCommandHandler> logger) : ICommandHandler<SubmitVerificationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(SubmitVerificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (userId == Guid.Empty)
                return Result.Failure<Guid>(Error.Forbidden, "Not authenticated.");

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result.Failure<Guid>(Error.Forbidden, "User not found.");

            if (user.Status != AccountStatus.PendingIdentityVerification)
            {
                return Result.Failure<Guid>(
                    Error.AccountNotActive,
                    "Account is not awaiting identity verification.");
            }

            if (request.Files is null || request.Files.Count == 0)
            {
                return Result.Failure<Guid>(Error.EmptyValue, "At least one document is required.");
            }

            // BR-39: only one pending request at a time
            var pendingExists = await verificationRepository
                .ExistsAsync(v => v.UserId == userId && v.Status == VerificationStatus.Pending, cancellationToken);

            if (pendingExists)
            {
                return Result.Failure<Guid>(
                    Error.VerificationAlreadyPending,
                    "You already have a pending verification request.");
            }

            // BR-30 / BR-31: validate every file BEFORE uploading anything so we don't leak orphan files.
            foreach (var file in request.Files)
            {
                var validation = fileValidator.Validate(file.FileName, file.FileSize, file.MimeType);
                if (validation.IsFailure)
                    return Result.Failure<Guid>(validation.Error ?? Error.InvalidValue, validation.Message);
            }

            var uploadedDocs = new List<VerificationDocument>();
            foreach (var file in request.Files)
            {
                var uploaded = await fileUploader.UploadAsync(file.Content, file.FileName, cancellationToken);
                if (string.IsNullOrWhiteSpace(uploaded.Url))
                {
                    return Result.Failure<Guid>(Error.ServerError, "File upload failed.");
                }

                uploadedDocs.Add(VerificationDocument.Create(
                    file.DocumentType,
                    uploaded.Url,
                    file.FileName,
                    file.FileSize,
                    file.MimeType));
            }

            var requestExpiryDays = configuration.GetValue("Verification:RequestExpiryDays", 14);
            var verificationRequest = VerificationRequest.Submit(
                userId,
                uploadedDocs,
                TimeSpan.FromDays(requestExpiryDays));

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await verificationRepository.AddAsync(verificationRequest, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            await businessNotificationService.NotifyAsync(
                NotificationTemplateKeys.VerificationSubmitted,
                userId,
                verificationRequest.Id,
                new Dictionary<string, string>
                {
                    ["referenceId"] = verificationRequest.Id.ToString()
                },
                new
                {
                    VerificationRequestId = verificationRequest.Id,
                    Status = verificationRequest.Status.ToString()
                },
                cancellationToken);

            return Result.Success(verificationRequest.Id, "Verification request submitted.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error submitting verification request");
            return Result.Failure<Guid>(Error.ServerError, "An error occurred while submitting the verification request.");
        }
    }
}
