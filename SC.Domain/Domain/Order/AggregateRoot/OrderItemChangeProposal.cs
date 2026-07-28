using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.Order.Enum;

namespace SC.Domain.Domain.Order.AggregateRoot;

public class OrderItemChangeProposal : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private OrderItemChangeProposal() { }

    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CurrentDishId { get; private set; }
    public Guid? SuggestedDishId { get; private set; }
    public Guid? SelectedDishId { get; private set; }
    public bool IsRequiredItem { get; private set; }
    public Guid? RequiredCategoryId { get; private set; }
    public ChangeProposalStatus ProposalStatus { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RespondedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static OrderItemChangeProposal Create(
        Guid orderId,
        Guid userId,
        Guid currentDishId,
        Guid? suggestedDishId,
        bool isRequiredItem,
        Guid? requiredCategoryId,
        DateTimeOffset expiresAtUtc)
    {
        return new OrderItemChangeProposal
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            UserId = userId,
            CurrentDishId = currentDishId,
            SuggestedDishId = suggestedDishId,
            IsRequiredItem = isRequiredItem,
            RequiredCategoryId = requiredCategoryId,
            ProposalStatus = ChangeProposalStatus.WaitingResponse,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = userId,
            UpdatedBy = userId
        };
    }

    public bool IsExpired(DateTimeOffset nowUtc)
    {
        return ProposalStatus == ChangeProposalStatus.WaitingResponse
               && ExpiresAtUtc <= nowUtc;
    }

    public void Accept(Guid selectedDishId, Guid updatedBy)
    {
        if (ProposalStatus != ChangeProposalStatus.WaitingResponse)
            throw new InvalidOperationException($"Cannot accept proposal in status {ProposalStatus}.");
        if (selectedDishId == Guid.Empty)
            throw new ArgumentException("Selected dish is required.", nameof(selectedDishId));

        SelectedDishId = selectedDishId;
        ProposalStatus = ChangeProposalStatus.Accepted;
        RespondedAtUtc = DateTimeOffset.UtcNow;
        Touch(updatedBy);
    }

    public void RequestRefund(Guid updatedBy)
    {
        if (ProposalStatus != ChangeProposalStatus.WaitingResponse)
            throw new InvalidOperationException($"Cannot request refund on proposal in status {ProposalStatus}.");
        if (IsRequiredItem)
            throw new InvalidOperationException("Required item cannot be refunded separately.");

        ProposalStatus = ChangeProposalStatus.RefundRequested;
        RespondedAtUtc = DateTimeOffset.UtcNow;
        Touch(updatedBy);
    }

    public void ReopenRefundRequest(Guid updatedBy)
    {
        if (ProposalStatus != ChangeProposalStatus.RefundRequested)
            throw new InvalidOperationException($"Cannot reopen refund request on proposal in status {ProposalStatus}.");

        ProposalStatus = ChangeProposalStatus.WaitingResponse;
        RespondedAtUtc = null;
        Touch(updatedBy);
    }

    public void ReopenOrderRefundRequest(Guid updatedBy)
    {
        if (ProposalStatus != ChangeProposalStatus.OrderRefundRequested)
            throw new InvalidOperationException($"Cannot reopen order refund request on proposal in status {ProposalStatus}.");

        ProposalStatus = ChangeProposalStatus.WaitingResponse;
        RespondedAtUtc = null;
        Touch(updatedBy);
    }

    public void RequestOrderRefund(Guid updatedBy)
    {
        if (ProposalStatus != ChangeProposalStatus.WaitingResponse)
            throw new InvalidOperationException($"Cannot request order refund on proposal in status {ProposalStatus}.");

        ProposalStatus = ChangeProposalStatus.OrderRefundRequested;
        RespondedAtUtc = DateTimeOffset.UtcNow;
        Touch(updatedBy);
    }

    private void Touch(Guid updatedBy)
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
