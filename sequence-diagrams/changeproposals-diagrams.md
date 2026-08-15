# ChangeProposalsController — Sequence Diagrams

Base route: **`/api/changeproposals`** · Auth: **JWT (the proposal's owner)** · API version `1.0`
Source: [`ChangeProposalsController.cs`](../SC.Api/Controllers/ChangeProposalsController.cs)

| Endpoint | Handler | Effect |
|---|---|---|
| `GET /api/changeproposals` | `GetMyChangeProposalsQueryHandler` | list the caller's open proposals |
| `GET /api/changeproposals/{id}` | `GetChangeProposalByIdQueryHandler` | one proposal + swap candidates |
| `POST /api/changeproposals/{id}/accept` | `AcceptChangeProposalCommandHandler` | swap the dish, order stays alive |
| `POST /api/changeproposals/{id}/request-refund` | `RequestRefundFromProposalCommandHandler` | refund **one item**, auto-credited |
| `POST /api/changeproposals/{id}/request-order-refund` | `RequestOrderRefundFromProposalCommandHandler` | cancel the order, refund **everything**, auto-credited |

**Where proposals come from:** `POST /api/sessions/{id}/finalize` creates one `OrderItemChangeProposal` per under-supplied order item and notifies the student ([`sessions-diagrams.md`](sessions-diagrams.md#2-post-apisessionsidfinalize--manual-finalize)). Each proposal carries an `ExpiresAtUtc` from the `CHANGE_PROPOSAL / RESPONSE / RESPONSE_WINDOW_MINUTES` setting (default 30 min).

**Layers:** `ChangeProposalsController → IMediator → Handler → IRefundLockService / IRefundAutoCreditService / IGenericRepository → SmartCanteenDbContext → PostgreSQL`

**Shared guards** — all three write endpoints reject in this order before doing any work:

| Guard | Message |
|---|---|
| proposal exists | `Proposal not found.` |
| `proposal.UserId == caller` | `This proposal does not belong to you.` |
| not past `ExpiresAtUtc` | `Change proposal has expired.` |
| order exists and `Status == Preparing` | `Order is no longer available for change proposal actions.` |

---

## 1. `POST /{id}/accept` — swap the dish

The replacement is re-validated against the **session**, the order's **meal template**, and the required-category rule — a swap may not turn a valid plate into an invalid one.

```plantuml
@startuml
title sd AcceptChangeProposal — POST /api/changeproposals/{id}/accept
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "student : Student" as STU
box "API" #F7F7F7
  participant "ctrl : ChangeProposalsController" as CTRL
  participant "h : AcceptChangeProposalCommandHandler" as H
end box
participant "rule : CartTemplateRuleValidator" as RULE
database "db : PostgreSQL" as DB
participant "noti : IBusinessNotificationService" as NOTI
actor "manager : Manager" as MGR

activate STU
STU -> CTRL ++: POST /api/changeproposals/{id}/accept(newDishId)
CTRL -> H ++: Send(AcceptChangeProposalCommand, ProposalId = route id) via IMediator

H -> DB ++: OrderItemChangeProposals.FirstOrDefaultAsync(p => p.Id == proposalId)
DB -->> H --: proposal | null
opt [proposal null | proposal.UserId != caller | past ExpiresAtUtc]
  H -->> CTRL: Failure(NullValue | InvalidValue, reason) : 400
end

H -> DB ++: Dishes.Where(d => d.Id == newDishId or d.Id == proposal.CurrentDishId)
DB -->> H --: newDish, currentDish
opt [new dish null | IsDeleted | !IsActive | current dish missing]
  H -->> CTRL: Failure(NullValue) : 400
end
opt [IsRequiredItem and newDish.CategoryId != proposal.RequiredCategoryId]
  H -->> CTRL: Failure(InvalidValue, "Required item must be swapped with a dish\nfrom the same required category.") : 400
end
opt [optional item and newDish.CategoryId != currentDish.CategoryId]
  H -->> CTRL: Failure(InvalidValue, "Optional item must be swapped with a dish\nfrom the same category.") : 400
end

H -> DB ++: Orders.FirstOrDefaultAsync(o => o.Id == proposal.OrderId && !o.IsDeleted)
DB -->> H --: order | null
opt [order null | order.Status != Preparing]
  H -->> CTRL: Failure(reason, "Order is no longer available for change proposal actions.") : 400
end

H -> DB ++: Sessions.Include(SessionDishes)\n.Include(MealTemplates).ThenInclude(Settings)\n.FirstOrDefaultAsync(s => s.Id == order.SessionId)
DB -->> H --: session | null
opt [session null | newDishId is not one of session.SessionDishes\n| the order item for CurrentDishId is missing]
  H -->> CTRL: Failure(NullValue | InvalidValue, reason) : 400
end

opt [the order has a meal template]
  H -> DB ++: Dishes.Where(d => validationDishIds.Contains(d.Id)).ToListAsync()
  DB -->> H --: validation dishes
  H -> RULE ++: Validate(template, items with the swap applied,\nrequireCompleteTemplate = true)
  note over H, RULE: refunded items are excluded, and so are other ChangePending items,\nso one unresolved proposal cannot block another
  RULE -->> H --: Result | Failure
  opt [the swap would break the template]
    H -->> CTRL: Failure(templateValidation.Error, message) : 400
  end
end

H -> H: proposal.Accept(newDishId, userId)\nitem.SwapDish(newDishId, newDish.Price.Amount)
H -> DB ++: update OrderItemChangeProposals.ProposalStatus = Accepted,\nupdate OrderItem.DishId, .UnitPrice_Amount, .ItemStatus = Swapped
deactivate DB
note over H, DB: no explicit transaction — SaveChangesAsync commits both updates\nin EF's implicit transaction. No money moves, so no wallet lock.

opt [the session creator is neither empty nor the caller]
  H -> NOTI ++: NotifyAsync(ChangeProposalAccepted, session.CreatedBy, proposal.Id)
  NOTI -->> MGR --: SignalR + FCM "student swapped a dish"
end

H -->> CTRL --: Success("Dish swapped successfully.")
CTRL -->> STU --: 200 OK
deactivate STU
@enduml
```

---

## 2. `POST /{id}/request-refund` — refund one item

Only for **optional** items. Money moves, so this runs in an explicit transaction behind a per-order advisory lock, and the refund is **auto-approved and credited immediately** — no manager step.

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
  deactivate DB
  deactivate LOCK

  H -> DB ++: RefundRequests.AnyAsync(active refund for this order / item / proposal)
  DB -->> H --: activeRequestExists
  opt [activeRequestExists]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(InvalidValue, "This order item already has a pending\nor approved refund request.") : 400
  end

  H -> H: proposal.RequestRefund(userId), item.MarkRefundPending()
  H -> DB ++: insert RefundRequest
  deactivate DB

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
  deactivate DB
end

H -> NOTI ++: NotifyAsync(RefundApproved, student, refundRequest.Id, { refundAmount })
NOTI -->> STU --: SignalR + FCM "item refunded"
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

---

## 3. `POST /{id}/request-order-refund` — cancel the order, refund everything

The largest handler in the codebase (279 LOC, 18 dependencies). It cancels the order, **closes every sibling proposal on the same order**, refunds the remaining (non-refunded) amount, and auto-credits it.

```plantuml
@startuml
title sd RequestOrderRefundFromProposal — POST /api/changeproposals/{id}/request-order-refund
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "student : Student" as STU
box "API" #F7F7F7
  participant "ctrl : ChangeProposalsController" as CTRL
  participant "h : RequestOrderRefundFromProposalCommandHandler" as H
end box
box "Domain services" #FAFAFA
  participant "uow : IUnitOfWork" as UOW
  participant "lock : IRefundLockService" as LOCK
  participant "credit : IRefundAutoCreditService" as CREDIT
end box
database "db : PostgreSQL" as DB
participant "noti : IBusinessNotificationService" as NOTI

activate STU
STU -> CTRL ++: POST /api/changeproposals/{id}/request-order-refund
CTRL -> H ++: Send(RequestOrderRefundFromProposalCommand) via IMediator

H -> DB ++: OrderItemChangeProposals.FirstOrDefaultAsync(p => p.Id == proposalId)\n+ Orders.FirstOrDefaultAsync(o => o.Id == proposal.OrderId)\n+ Sessions.FirstOrDefaultAsync(s => s.Id == order.SessionId)
DB -->> H --: proposal + order + session
opt [proposal null | not owned by the caller | expired\n| order null | order.Status != Preparing | session null]
  H -->> CTRL: Failure(NullValue | InvalidValue, reason) : 400
end

critical transaction + per-order advisory lock
  H -> UOW ++: BeginTransactionAsync()
  deactivate UOW
  H -> LOCK ++: LockOrderRefundRequestsAsync(order.Id)
  LOCK -> DB ++: SELECT pg_advisory_xact_lock(hashtext('refund-order-{orderId}'))
  deactivate DB
  deactivate LOCK

  H -> DB ++: RefundRequests.AnyAsync(order-level refund, no OrderItemId,\nStatus Pending or Approved)
  DB -->> H --: activeOrderRefundExists
  opt [activeOrderRefundExists]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(InvalidValue, "This order already has a pending\nor approved full order refund request.") : 400
  end

  H -> DB ++: Settings CHANGE_PROPOSAL/REFUND/ORDER_REFUND_POLICY_CODE,\nthen REFUND_POLICY rows for that scope
  DB -->> H --: policy | invalid
  opt [policy invalid | policy.RequiresImage]
    H -->> CTRL: Failure(InvalidValue, reason) : 400
  end
  opt [orderAmount over non-refunded items <= 0]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(InvalidValue, "Remaining order amount must be greater than zero.") : 400
  end

  H -> H: refundRequest = RefundRequest.Submit(...) + AttachProposalContext(null, proposal.Id, currentDishId)\nproposal.RequestOrderRefund(userId)

  H -> DB ++: OrderItemChangeProposals.Where(p => p.OrderId == order.Id\n&& p.Id != proposal.Id && p.ProposalStatus == WaitingResponse)
  DB -->> H --: sibling proposals
  loop for each sibling
    H -> H: sibling.RequestOrderRefund(userId) — the whole order is going back
  end

  H -> H: order.UpdateStatus(Cancelled, userId)
  H -> DB ++: insert OrderStatusHistory(from -> Cancelled, "ChangeProposalOrderRefund")\n+ insert RefundRequest
  deactivate DB

  ref over H, CREDIT, DB
    CreditRefund(refundRequest)
    lock the user FOR UPDATE, credit the balance,
    insert WalletTransaction (Type = Refund), refund.AutoApprove(txId)
  end ref
  opt [credit failed]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(credit error) : 400
  end

  H -> DB ++: update proposal + siblings + order, then Commit
  deactivate DB
end

H -> NOTI ++: NotifyAsync(RefundApproved, student, refundRequest.Id, { refundAmount })
NOTI -->> STU --: SignalR + FCM "order cancelled and refunded"
opt [the session creator is neither empty nor the caller]
  H -> NOTI ++: NotifyAsync(manager, "order refund requested")
  deactivate NOTI
end

H -->> CTRL --: Success("Full order refund approved and credited automatically.")
CTRL -->> STU --: 200 OK
deactivate STU

note over H, DB: InvalidOperationException -> Rollback + Failure(InvalidValue, ex.Message)\nany other exception -> Rollback + Failure(ServerError) : 400
@enduml
```

---

## 4. Background: the student never answers

`ChangeProposalExpirationJob` sweeps proposals whose `ExpiresAtUtc` has passed while still `WaitingResponse`. Each proposal runs in **its own transaction** so one failure does not poison the sweep.
Source: [`ChangeProposalExpirationService.cs`](../SC.Persistence/Database/Services/ChangeProposalExpirationService.cs)

```plantuml
@startuml
title sd ProcessExpiredProposals — ChangeProposalExpirationJob
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

participant "job : ChangeProposalExpirationJob" as JOB
participant "svc : ChangeProposalExpirationService" as SVC
box "Domain services" #FAFAFA
  participant "uow : IUnitOfWork" as UOW
  participant "lock : IRefundLockService" as LOCK
  participant "credit : IRefundAutoCreditService" as CREDIT
end box
database "db : PostgreSQL" as DB
participant "noti : IBusinessNotificationService" as NOTI
actor "student : Student" as STU

JOB -> SVC ++: ProcessExpiredProposalsAsync()
SVC -> DB ++: OrderItemChangeProposals.Where(p => p.ProposalStatus == WaitingResponse\n&& p.ExpiresAtUtc <= now).ToListAsync()
DB -->> SVC --: expired proposals

loop for each expired proposal — its own transaction
  SVC -> DB ++: Orders.Include(OrderItems).FirstOrDefaultAsync(o => o.Id == proposal.OrderId)
  DB -->> SVC --: order | null
  opt [order null | order.Status != Preparing]
    SVC -> SVC: skip — the order has already moved on
  end

  critical per-proposal transaction + per-order advisory lock
    SVC -> UOW ++: BeginTransactionAsync()
    deactivate UOW
    SVC -> LOCK ++: LockOrderRefundRequestsAsync(order.Id)
    LOCK -> DB ++: SELECT pg_advisory_xact_lock(hashtext('refund-order-{orderId}'))
    deactivate DB
    deactivate LOCK

    alt [proposal.IsRequiredItem — the plate cannot be completed]
      SVC -> DB ++: any active order-level refund already?
      DB -->> SVC --: exists
      opt [a refund already exists | orderAmount <= 0]
        SVC -> UOW: RollbackAsync()
      end
      SVC -> DB ++: insert RefundRequest.Submit(configured order policy,\nfull amount, "...proposal expired")
      deactivate DB
      ref over SVC, CREDIT, DB : CreditRefund(refundRequest)
    else [optional item — drop just that item]
      SVC -> DB ++: resolve the item refund policy,\ninsert RefundRequest for that item only
      deactivate DB
      ref over SVC, CREDIT, DB : CreditRefund(refundRequest)
    end

    opt [credit failed]
      SVC -> UOW: RollbackAsync() — logged, the sweep continues
    end
    SVC -> DB ++: SaveChanges + Commit
    deactivate DB
  end
  SVC -> SVC: queue the student notification
end

loop for each queued notification
  SVC -> NOTI ++: NotifyAsync(...)
  NOTI -->> STU --: SignalR + FCM
end
deactivate SVC

note over SVC: one failing proposal is rolled back and logged —\nit never poisons the rest of the sweep
@enduml
```

---

## Proposal state

```plantuml
@startuml
title Proposal state
hide empty description

[*] --> WaitingResponse : session finalize raises the proposal
WaitingResponse --> Accepted : POST /accept — dish swapped
WaitingResponse --> RefundRequested : POST /request-refund — item refunded and credited
WaitingResponse --> OrderRefundRequested : POST /request-order-refund — order cancelled and refunded
WaitingResponse --> OrderRefundRequested : sibling proposal triggered a full order refund
WaitingResponse --> RefundRequested : expiration job, optional item
WaitingResponse --> OrderRefundRequested : expiration job, required item
RefundRequested --> WaitingResponse : manager rejects the refund (ReopenRefundRequest)
OrderRefundRequested --> WaitingResponse : manager rejects the refund (ReopenOrderRefundRequest)
Accepted --> [*]
@enduml
```

> The two "reopen" edges come from [`refundmanager-diagrams.md`](refundmanager-diagrams.md) — rejecting a proposal-linked refund puts the proposal back in front of the student and restores the order to `Preparing`.

---

## Related

- [`refunds-diagrams.md`](refunds-diagrams.md) · [`refundmanager-diagrams.md`](refundmanager-diagrams.md) · [`cart-diagrams.md`](cart-diagrams.md)
- [`API-Modules/change-proposal-api.md`](../API-Modules/change-proposal-api.md) — request/response reference
- [`API-Modules/change-proposal-refund-flow.md`](../API-Modules/change-proposal-refund-flow.md) — prose walkthrough of the same flow
