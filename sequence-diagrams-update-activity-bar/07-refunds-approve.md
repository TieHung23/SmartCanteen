# 7 · POST /api/manager/refunds/{id}/approve

Corrected copy of `sd ApproveRefundRequest — POST /api/manager/refunds/{id}/approve` from [`../sequence-diagrams/refundmanager-diagrams.md`](../sequence-diagrams/refundmanager-diagrams.md).

## What changed

- DB reply added after «SELECT 1 FROM "RefundRequests" WHERE "Id" = @id FOR »
- DB reply added after «SELECT 1 FROM "Users" WHERE "Id" = @userId FOR UPDAT»
- DB reply added after «insert WalletTransaction (Type = Refund),\nupdate Re»
- NOTI now returns to H and pushes to STU asynchronously

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
  DB -->> LOCK --: lock granted — blocks until the row/key is free
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
  DB -->> WAL --: lock granted — blocks until the row/key is free
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
  DB -->> H --: rows written
  H -> UOW ++: CommitAsync()
  deactivate UOW
end

H -> NOTI ++: NotifyAsync(RefundApproved, refund.UserId, refund.Id, { refundAmount })
NOTI -->> H --: queued
NOTI ->> STU: SignalR + FCM "refund approved, wallet credited"
H -->> CTRL --: Success({ id, walletTransactionId, refundAmount, balanceAfter, status })
CTRL -->> MGR --: 200 OK
deactivate MGR

note over H, DB: InvalidOperationException -> Rollback + Failure(InvalidValue, ex.Message)\nany other exception -> Rollback + Failure(ServerError) : 400
@enduml
```
