# 8 · POST /api/changeproposals/{id}/request-refund

Corrected copy of `sd RequestItemRefundFromProposal — POST /api/changeproposals/{id}/request-refund` from [`../sequence-diagrams/changeproposals-diagrams.md`](../sequence-diagrams/changeproposals-diagrams.md).

## What changed

- DB reply added after «SELECT pg_advisory_xact_lock(hashtext('refund-order-»
- DB reply added after «insert RefundRequest»
- DB reply added after «update proposal + order item, then Commit»
- NOTI now returns to H and pushes to STU asynchronously

```plantuml
@startuml
title sd RequestItemRefundFromProposal — POST /api/changeproposals/{id}/request-refund
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "student : Student" as STU
box "API" #F7F7F7
  participant "ctrl : ChangeProposalsController" as CTRL
  participant "h : RequestRefundFromProposalCommandHandler" as H
end box
box "Domain services" #FAFAFA
  participant "uow : IUnitOfWork" as UOW
  participant "lock : IRefundLockService" as LOCK
  participant "credit : IRefundAutoCreditService" as CREDIT
end box
database "db : PostgreSQL" as DB
participant "noti : IBusinessNotificationService" as NOTI

activate STU
STU -> CTRL ++: POST /api/changeproposals/{id}/request-refund
CTRL -> H ++: Send(RequestRefundFromProposalCommand) via IMediator

H -> DB ++: OrderItemChangeProposals.FirstOrDefaultAsync(p => p.Id == proposalId)
DB -->> H --: proposal | null
opt [proposal null | not owned by the caller | past ExpiresAtUtc]
  H -->> CTRL: Failure(NullValue | InvalidValue, reason) : 400
end
opt [proposal.IsRequiredItem]
  H -->> CTRL: Failure(InvalidValue, "Required item cannot be refunded separately.\nPlease swap item or request a full order refund.") : 400
end

H -> DB ++: Orders.FirstOrDefaultAsync(o => o.Id == proposal.OrderId)\n+ Sessions.FirstOrDefaultAsync(s => s.Id == order.SessionId)
DB -->> H --: order + session | null
opt [order null | order.Status != Preparing | session null\n| the order item for CurrentDishId is missing]
  H -->> CTRL: Failure(NullValue | InvalidValue, reason) : 400
end

H -> DB ++: Settings CHANGE_PROPOSAL/REFUND/ITEM_REFUND_POLICY_CODE,\nthen REFUND_POLICY rows for that scope\n-> RefundPolicyDefinition.TryCreate
DB -->> H --: policy | invalid
opt [policy not configured or invalid]
  H -->> CTRL: Failure(InvalidValue, "...item refund policy is not configured\n/ not active or invalid.") : 400
end
opt [policy.RequiresImage]
  H -->> CTRL: Failure(InvalidValue, "...item refund policy cannot require images.") : 400
end
opt [itemAmount <= 0]
  H -->> CTRL: Failure(InvalidValue, "Order item amount must be greater than zero.") : 400
end

H -> H: refundRequest = RefundRequest.Submit(orderId, userId, policy, itemAmount, reason)\n+ AttachProposalContext(item.Id, proposal.Id, item.DishId)

critical transaction + per-order advisory lock
  H -> UOW ++: BeginTransactionAsync()
  deactivate UOW
  H -> LOCK ++: LockOrderRefundRequestsAsync(order.Id)
  LOCK -> DB ++: SELECT pg_advisory_xact_lock(hashtext('refund-order-{orderId}'))
  DB -->> LOCK --: lock granted — blocks until the row/key is free
  deactivate LOCK

  H -> DB ++: RefundRequests.AnyAsync(active refund for this order / item / proposal)
  DB -->> H --: activeRequestExists
  opt [activeRequestExists]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(InvalidValue, "This order item already has a pending\nor approved refund request.") : 400
  end

  H -> H: proposal.RequestRefund(userId), item.MarkRefundPending()
  H -> DB ++: insert RefundRequest
  DB -->> H --: rows written

  ref over H, CREDIT, DB
    CreditRefund(refundRequest)
    lock the user FOR UPDATE, credit the balance,
    insert WalletTransaction (Type = Refund), refund.AutoApprove(txId)
  end ref
  opt [credit failed]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(credit error) : 400
  end

  H -> H: item.CompleteRefund()
  H -> DB ++: update proposal + order item, then Commit
  DB -->> H --: committed
end

H -> NOTI ++: NotifyAsync(RefundApproved, student, refundRequest.Id, { refundAmount })
NOTI -->> H --: queued
NOTI ->> STU: SignalR + FCM "item refunded"
opt [the session creator is neither empty nor the caller]
  H -> NOTI ++: NotifyAsync(ChangeProposalItemRefundRequested, session.CreatedBy)
  deactivate NOTI
end

H -->> CTRL --: Success("Item refund approved and credited automatically.")
CTRL -->> STU --: 200 OK
deactivate STU

note over H, DB: no manager step — the refund is created Approved and credited in one call
@enduml
```
