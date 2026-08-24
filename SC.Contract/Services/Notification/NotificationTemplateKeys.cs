namespace SC.Contract.Services.Notification;

public static class NotificationTemplateKeys
{
    public const string OrderCreated = "OrderCreated";
    public const string OrderStatusChanged = "OrderStatusChanged";
    public const string PaymentCompleted = "PaymentCompleted";
    public const string RefundSubmitted = "RefundSubmitted";
    public const string RefundApproved = "RefundApproved";
    public const string RefundRejected = "RefundRejected";
    public const string SessionDeletedRefund = "SessionDeletedRefund";
    public const string ChangeProposalCreated = "ChangeProposalCreated";
    public const string ChangeProposalAccepted = "ChangeProposalAccepted";
    public const string ChangeProposalItemRefundRequested = "ChangeProposalItemRefundRequested";
    public const string ChangeProposalOrderRefundRequested = "ChangeProposalOrderRefundRequested";
    public const string SessionAutoRejected = "SessionAutoRejected";
    public const string SessionAutoConfirmed = "SessionAutoConfirmed";
    public const string VerificationSubmitted = "VerificationSubmitted";
    public const string VerificationApproved = "VerificationApproved";
    public const string VerificationRejected = "VerificationRejected";
    public const string ServingFailed = "ServingFailed";
    public const string OrderReadyForPickup = "OrderReadyForPickup";
    public const string OrderServingIssue = "OrderServingIssue";
    public const string OrderPreparing = "OrderPreparing";
    public const string OrderCollectedStaff = "OrderCollectedStaff";
    public const string OrderAssembledStaff = "OrderAssembledStaff";
}
