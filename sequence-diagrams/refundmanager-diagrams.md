# RefundManagerController — Sequence Diagrams

Base route: **`/api/manager/refunds`** · Auth: **JWT, role `Manager`** · API version `1.0`
Source: [`RefundManagerController.cs`](../SC.Api/Controllers/RefundManagerController.cs)

| Endpoint | Handler | Effect |
|---|---|---|
| `GET /api/manager/refunds` | `GetRefundRequestsQueryHandler` | queue of requests, filterable by status |
| `GET /api/manager/refunds/{id}` | `GetRefundRequestDetailQueryHandler` | one request + order + evidence images |
| `POST /api/manager/refunds/{id}/approve` | `ApproveRefundRequestCommandHandler` | **credits the wallet** |
| `POST /api/manager/refunds/{id}/reject` | `RejectRefundRequestCommandHandler` | records a reason and **undoes** the side effects |

Both write endpoints open the transaction **first** and take a `FOR UPDATE` lock on the refund row **before** reading it — two managers clicking Approve at the same moment serialize, and the loser sees `Refund request is no longer pending.`

**Layers:** `RefundManagerController → IMediator → Handler → IRefundLockService / IWalletDomainService / IGenericRepository → SmartCanteenDbContext → PostgreSQL`

---

## 1. `POST /{id}/approve` — credit the wallet

```plantuml
@startuml
title sd ApproveRefundRequest — POST /api/manager/refunds/{id}/approve
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "manager : Manager" as MGR
box "API" #F7F7F7
  participant "ctrl : RefundManagerController" as CTRL
  participant "h : ApproveRefundRequestCommandHandler" as H
end box
box "Domain services" #FAFAFA
  participant "uow : IUnitOfWork" as UOW
  participant "lock : IRefundLockService" as LOCK
  participant "wallet : IWalletDomainService" as WAL
end box
database "db : PostgreSQL" as DB
participant "noti : IBusinessNotificationService" as NOTI
actor "student : Student" as STU

activate MGR
MGR -> CTRL ++: POST /api/manager/refunds/{id}/approve
CTRL -> H ++: Send(ApproveRefundRequestCommand(id)) via IMediator

critical transaction + refund row lock + per-user row lock
  H -> UOW ++: BeginTransactionAsync()
  deactivate UOW
  H -> LOCK ++: LockRefundRequestAsync(id)
  LOCK -> DB ++: SELECT 1 FROM "RefundRequests" WHERE "Id" = @id FOR UPDATE
  deactivate DB
  deactivate LOCK
  note over LOCK, DB: two managers clicking Approve at the same moment serialize here;\nthe loser sees "Refund request is no longer pending."

  H -> DB ++: RefundRequests.FirstOrDefaultAsync(r => r.Id == id)
  DB -->> H --: refund | null
  opt [refund null | IsDeleted | Status != Pending]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(NullValue, "Refund request not found.")\n| Failure(InvalidValue, "Refund request is no longer pending.") : 400
  end

  H -> WAL ++: LockUserAsync(refund.UserId)
  WAL -> DB ++: SELECT 1 FROM "Users" WHERE "Id" = @userId FOR UPDATE
  deactivate DB
  deactivate WAL
  H -> DB ++: Users.FirstOrDefaultAsync(u => u.Id == refund.UserId)
  DB -->> H --: user | null -> balanceBefore = user.Balance.Amount
  opt [user null | IsDeleted]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(NullValue, "Refund request user not found.") : 400
  end

  H -> WAL ++: TryCreditUserBalanceAsync(user.Id, refund.RefundAmount)
  WAL -> DB ++: UPDATE "Users" SET "Balance_Amount" = "Balance_Amount" + @amount\nWHERE "Id" = @userId RETURNING "Balance_Amount"
  DB -->> WAL --: balanceAfter | Failure
  WAL -->> H --: balanceAfter
  opt [credit failed]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(credit error) : 400
  end

  H -> H: walletTransaction = WalletTransaction.Create(userId, +RefundAmount,\nbalanceBefore, balanceAfter, Type = Refund)\nrefund.Approve(managerId, walletTransaction.Id)

  opt [refund.OrderItemId is set — the refund targets one item]
    H -> DB ++: Orders.Include(OrderItems).FirstOrDefaultAsync(o => o.Id == refund.OrderId)
    DB -->> H --: order
    opt [the item is missing]
      H -> UOW: RollbackAsync()
      H -->> CTRL: Failure(NullValue, "Refund request order item not found.") : 400
    end
    H -> H: item.CompleteRefund() -> ItemStatus = Refunded
  end

  H -> DB ++: insert WalletTransaction (Type = Refund),\nupdate RefundRequest.Status = Approved, .WalletTransactionId, .ReviewedBy,\nupdate OrderItem.ItemStatus = Refunded when item-scoped
  deactivate DB
  H -> UOW ++: CommitAsync()
  deactivate UOW
end

H -> NOTI ++: NotifyAsync(RefundApproved, refund.UserId, refund.Id, { refundAmount })
NOTI -->> STU --: SignalR + FCM "refund approved, wallet credited"
H -->> CTRL --: Success({ id, walletTransactionId, refundAmount, balanceAfter, status })
CTRL -->> MGR --: 200 OK
deactivate MGR

note over H, DB: InvalidOperationException -> Rollback + Failure(InvalidValue, ex.Message)\nany other exception -> Rollback + Failure(ServerError) : 400
@enduml
```

---

## 2. `POST /{id}/reject` — record a reason and unwind

Rejection is not just a status flip. What it undoes depends on **what the refund was attached to** — and rejecting a proposal-linked order refund brings the cancelled order back to life.

```plantuml
@startuml
title sd RejectRefundRequest — POST /api/manager/refunds/{id}/reject
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "manager : Manager" as MGR
box "API" #F7F7F7
  participant "ctrl : RefundManagerController" as CTRL
  participant "h : RejectRefundRequestCommandHandler" as H
end box
box "Domain services" #FAFAFA
  participant "uow : IUnitOfWork" as UOW
  participant "lock : IRefundLockService" as LOCK
end box
database "db : PostgreSQL" as DB
participant "noti : IBusinessNotificationService" as NOTI
actor "student : Student" as STU

activate MGR
MGR -> CTRL ++: POST /api/manager/refunds/{id}/reject(reason)
CTRL -> H ++: Send(RejectRefundRequestCommand(id, reason)) via IMediator

critical transaction + refund row lock
  H -> UOW ++: BeginTransactionAsync()
  deactivate UOW
  H -> LOCK ++: LockRefundRequestAsync(id)
  LOCK -> DB ++: SELECT 1 FROM "RefundRequests" WHERE "Id" = @id FOR UPDATE
  deactivate DB
  deactivate LOCK

  H -> DB ++: RefundRequests.FirstOrDefaultAsync(r => r.Id == id)
  DB -->> H --: refund | null
  opt [refund null | IsDeleted | Status != Pending]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(reason) : 400
  end

  H -> H: refund.Reject(managerId, reason)

  alt [OrderItemId set — item-scoped refund]
    H -> DB ++: Orders.Include(OrderItems).FirstOrDefaultAsync(o => o.Id == refund.OrderId)
    DB -->> H --: order
    opt [the item is missing]
      H -> UOW: RollbackAsync()
      H -->> CTRL: Failure(NullValue, "Refund request order item not found.") : 400
    end
    H -> H: item.CancelRefund() — RefundPending returns to its previous state
  else [ChangeProposalId set, no OrderItemId — full order refund from a proposal]
    H -> DB ++: Orders.FirstOrDefaultAsync(o => o.Id == refund.OrderId)
    DB -->> H --: order | null
    opt [order is missing]
      H -> UOW: RollbackAsync()
      H -->> CTRL: Failure(NullValue, "Refund request order not found.") : 400
    end
    opt [order.Status == Cancelled]
      H -> H: order.UpdateStatus(Preparing, managerId) — the order is revived
      H -> DB ++: insert OrderStatusHistory(Cancelled -> Preparing,\n"ChangeProposalOrderRefundRejected")
      deactivate DB
    end
    H -> DB ++: OrderItemChangeProposals.Where(p => p.OrderId == refund.OrderId\n&& p.ProposalStatus == OrderRefundRequested).ToListAsync()
    DB -->> H --: sibling proposals
    loop for each such proposal
      H -> H: proposal.ReopenOrderRefundRequest(managerId) -> WaitingResponse
    end
  end

  opt [OrderItemId AND ChangeProposalId both set]
    H -> DB ++: OrderItemChangeProposals.FirstOrDefaultAsync(p => p.Id == refund.ChangeProposalId)
    DB -->> H --: proposal
    H -> H: proposal.ReopenRefundRequest(managerId) -> WaitingResponse
  end

  H -> DB ++: update RefundRequest.Status = Rejected + RejectReason,\nplus the OrderItem / Order / proposal changes unwound above
  deactivate DB
  H -> UOW ++: CommitAsync()
  deactivate UOW
end

H -> NOTI ++: NotifyAsync(RefundRejected, refund.UserId, refund.Id, { reason })
NOTI -->> STU --: SignalR + FCM "refund rejected"
H -->> CTRL --: Success(rejection result)
CTRL -->> MGR --: 200 OK
deactivate MGR

note over H, DB: rejection is not a status flip — what it undoes depends on\nwhether the refund was item-scoped, proposal-linked, or both
@enduml
```

**The three unwind shapes, side by side**

| Refund shape | `OrderItemId` | `ChangeProposalId` | What reject undoes |
|---|---|---|---|
| Plain order refund (student-submitted) | — | — | status only |
| Item refund | set | — | `item.CancelRefund()` |
| Item refund from a proposal | set | set | `item.CancelRefund()` **and** proposal back to `WaitingResponse` |
| Full order refund from a proposal | — | set | order `Cancelled → Preparing` + history, **every** `OrderRefundRequested` proposal on that order back to `WaitingResponse` |

> Note the asymmetry with change-proposal refunds: those are **auto-approved and credited on creation**, so they never sit in `Pending` and a manager never sees them in this queue. The reject paths above exist for proposal-linked refunds that were created `Pending` by other routes.

---

## 3. `GET` — queue and detail

```plantuml
@startuml
title sd ReadRefundQueue — GET /api/manager/refunds and /{id}
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "manager : Manager" as MGR
box "API" #F7F7F7
  participant "ctrl : RefundManagerController" as CTRL
  participant "h : GetRefundRequests / GetRefundRequestDetail handler" as H
end box
database "db : PostgreSQL" as DB

alt [queue]
activate MGR
  MGR -> CTRL ++: GET /api/manager/refunds?status=Pending&pageNumber=1
  CTRL -> H ++: Send(GetRefundRequestsQuery) via IMediator
  H -> DB ++: RefundRequests.Where(@status == null or r.Status == @status)\n.OrderByDescending(r => r.CreatedAtUtc).Skip(...).Take(...).ToListAsync()\n+ CountAsync()
  DB -->> H --: page + total
  H -> DB ++: Users for the requesters, OrderItemChangeProposals,\nDishes for the display names
  DB -->> H --: requesters + proposals + dishes
  H -->> CTRL --: Success(PaginatedList)
  CTRL -->> MGR --: 200 OK
else [detail]
  MGR -> CTRL ++: GET /api/manager/refunds/{id}
  CTRL -> H ++: Send(GetRefundRequestDetailQuery(id)) via IMediator
  H -> DB ++: RefundRequests.Include(Images).FirstOrDefaultAsync(r => r.Id == id)
  DB -->> H --: refund + evidence | null
  opt [not found]
    H -->> CTRL: Failure(NullValue) : 404
  end
  H -> DB ++: the requester, the linked change proposal, the dish
  DB -->> H --: requester + proposal + dish
  H -->> CTRL --: Success(detail)
  CTRL -->> MGR --: 200 OK
deactivate MGR
end
@enduml
```

---

## Related

- [`refunds-diagrams.md`](refunds-diagrams.md) — how a request gets into this queue
- [`changeproposals-diagrams.md`](changeproposals-diagrams.md) — the auto-credited refunds that bypass this queue, and the proposal states this controller can reopen
- [`API-Modules/refund-api.md`](../API-Modules/refund-api.md) — request/response reference
