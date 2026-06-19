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
    public ChangeProposalStatus ProposalStatus { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static OrderItemChangeProposal Create(
        Guid orderId,
        Guid userId,
        Guid currentDishId,
        Guid? suggestedDishId)
    {
        return new OrderItemChangeProposal
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            UserId = userId,
            CurrentDishId = currentDishId,
            SuggestedDishId = suggestedDishId,
            ProposalStatus = ChangeProposalStatus.WaitingResponse,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = userId,
            UpdatedBy = userId
        };
    }

    public void Accept(Guid updatedBy)
    {
        if (ProposalStatus != ChangeProposalStatus.WaitingResponse)
            throw new InvalidOperationException($"Cannot accept proposal in status {ProposalStatus}.");
        ProposalStatus = ChangeProposalStatus.Accepted;
        Touch(updatedBy);
    }

    public void RequestRefund(Guid updatedBy)
    {
        if (ProposalStatus != ChangeProposalStatus.WaitingResponse)
            throw new InvalidOperationException($"Cannot request refund on proposal in status {ProposalStatus}.");
        ProposalStatus = ChangeProposalStatus.RefundRequested;
        Touch(updatedBy);
    }

    private void Touch(Guid updatedBy)
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
