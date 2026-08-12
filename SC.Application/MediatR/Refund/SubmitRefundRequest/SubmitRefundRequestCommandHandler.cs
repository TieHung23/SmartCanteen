using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Application.MediatR.RefundPolicy;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Storage;
using SC.Contract.Services.Verification;
using SC.Contract.Shared;
using SC.Contract.Services.Notification;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Entity;
using SC.Domain.Domain.Refund.Enum;
using OrderAggregate = SC.Domain.Domain.Order.AggregateRoot.Order;
using SettingAggregate = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Application.MediatR.Refund.SubmitRefundRequest;

internal sealed class SubmitRefundRequestCommandHandler(
    IGenericRepository<OrderAggregate, Guid> orderRepository,
    IGenericRepository<SettingAggregate, Guid> settingRepository,
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IFileValidator fileValidator,
    IFileUploader fileUploader,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IBusinessNotificationService businessNotificationService,
    ILogger<SubmitRefundRequestCommandHandler> logger)
    : ICommandHandler<SubmitRefundRequestCommand, SubmitRefundRequestResponse>
{
    public async Task<Result<SubmitRefundRequestResponse>> Handle(
        SubmitRefundRequestCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                return Result.Failure<SubmitRefundRequestResponse>(
                    Error.Forbidden,
                    "Not authenticated.");
            }

            var order = await orderRepository
                .FindSingleAsync(order =>
                    !order.IsDeleted
                    && order.Id == request.OrderId
                    && order.CreatedBy == userId,
                    cancellationToken);

            if (order is null)
            {
                return Result.Failure<SubmitRefundRequestResponse>(
                    Error.NullValue,
                    "Order not found or does not belong to the current user.");
            }

            // A cancelled or expired order is already closed out - whatever refund it was owed was
            // settled by the flow that closed it, so it cannot take a fresh request.
            if (order.Status is OrderStatus.Cancelled or OrderStatus.Expired)
            {
                return Result.Failure<SubmitRefundRequestResponse>(
                    Error.InvalidValue,
                    "A cancelled or expired order cannot be refunded.");
            }

            var activeRequestExists = await refundRepository
                .ExistsAsync(refund =>
                    !refund.IsDeleted
                    && refund.OrderId == order.Id
                    && (refund.Status == RefundRequestStatus.Pending
                        || refund.Status == RefundRequestStatus.Approved),
                    cancellationToken);

            if (activeRequestExists)
            {
                return Result.Failure<SubmitRefundRequestResponse>(
                    Error.InvalidValue,
                    "This order already has a pending or approved refund request.");
            }

            var normalizedPolicyCode = request.PolicyCode.Trim();
            var policySettings = await settingRepository
                .FindListAsync(setting =>
                    !setting.IsDeleted
                    && setting.Group.ToUpper() == RefundPolicyConstants.Group
                    && setting.Scope.ToLower() == normalizedPolicyCode.ToLower(),
                    cancellationToken);

            if (!RefundPolicyDefinition.TryCreate(
                    normalizedPolicyCode,
                    policySettings,
                    out var policy))
            {
                return Result.Failure<SubmitRefundRequestResponse>(
                    Error.InvalidValue,
                    "Refund policy is not active or invalid.");
            }

            if (policy!.RequiresImage && request.Images.Count == 0)
            {
                return Result.Failure<SubmitRefundRequestResponse>(
                    Error.EmptyValue,
                    "At least one image is required for this refund policy.");
            }

            foreach (var image in request.Images)
            {
                if (!image.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    return Result.Failure<SubmitRefundRequestResponse>(
                        Error.UnsupportedFileFormat,
                        "Refund evidence must be an image.");
                }

                var validation = fileValidator.Validate(
                    image.FileName,
                    image.FileSize,
                    image.MimeType);

                if (validation.IsFailure)
                {
                    return Result.Failure<SubmitRefundRequestResponse>(
                        validation.Error ?? Error.InvalidValue,
                        validation.Message);
                }
            }

            var orderAmount = order.OrderItems.Sum(
                item => item.UnitPrice.Amount * item.Quantity);

            if (orderAmount <= 0)
            {
                return Result.Failure<SubmitRefundRequestResponse>(
                    Error.InvalidValue,
                    "Order amount must be greater than zero.");
            }

            var refundRequest = RefundRequest.Submit(
                order.Id,
                userId,
                policy.Scope,
                policy.Name,
                policy.Percent,
                orderAmount,
                request.Description);

            foreach (var image in request.Images)
            {
                var upload = await fileUploader.UploadAsync(
                    image.Content,
                    image.FileName,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(upload.Url))
                {
                    return Result.Failure<SubmitRefundRequestResponse>(
                        Error.ServerError,
                        "Refund image upload failed.");
                }

                refundRequest.AddImage(
                    RefundRequestImage.Create(
                        upload.Url,
                        image.FileName,
                        userId));
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await refundRepository.AddAsync(refundRequest, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new SubmitRefundRequestResponse
            {
                Id = refundRequest.Id,
                OrderId = refundRequest.OrderId,
                OrderItemId = refundRequest.OrderItemId,
                ChangeProposalId = refundRequest.ChangeProposalId,
                DishId = refundRequest.DishId,
                PolicyCode = refundRequest.PolicyCode,
                PolicyName = refundRequest.PolicyNameSnapshot,
                RefundPercent = refundRequest.RefundPercentSnapshot,
                OrderAmount = refundRequest.OrderAmountSnapshot,
                RefundAmount = refundRequest.RefundAmount,
                Status = refundRequest.Status.ToString(),
                ImageUrls = refundRequest.Images.Select(image => image.ImageUrl).ToList(),
                CreatedAtUtc = refundRequest.CreatedAtUtc
            };

            await businessNotificationService.NotifyAsync(
                NotificationTemplateKeys.RefundSubmitted,
                userId,
                refundRequest.Id,
                new Dictionary<string, string>
                {
                    ["referenceId"] = refundRequest.Id.ToString(),
                    ["refundAmount"] = refundRequest.RefundAmount.ToString("0.##")
                },
                new
                {
                    RefundRequestId = refundRequest.Id,
                    refundRequest.OrderId,
                    refundRequest.RefundAmount,
                    Status = refundRequest.Status.ToString()
                },
                cancellationToken);

            return Result.Success(response, "Refund request submitted successfully.");
        }
        catch (DbUpdateException ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogWarning(
                ex,
                "Duplicate refund request detected for order {OrderId}",
                request.OrderId);
            return Result.Failure<SubmitRefundRequestResponse>(
                Error.InvalidValue,
                "This order already has a pending or approved refund request.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(
                ex,
                "Error submitting refund request for order {OrderId}",
                request.OrderId);
            return Result.Failure<SubmitRefundRequestResponse>(
                Error.ServerError,
                "An error occurred while submitting the refund request.");
        }
    }
}
