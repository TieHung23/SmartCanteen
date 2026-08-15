# 5 · POST /api/orders

Corrected copy of `sd CreateOrder — POST /api/orders` from [`../sequence-diagrams/orders-diagrams.md`](../sequence-diagrams/orders-diagrams.md).

## What changed

- DB reply added after «SELECT 1 FROM "Users" WHERE "Id" = @userId FOR UPDAT»
- DB reply added after «orderRepository.AddAsync(order) + walletTransactionR»
- NOTI now returns to H and pushes to STU asynchronously

```plantuml
@startuml
title sd CreateOrder — POST /api/orders
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "student : Student" as STU
box "API" #F7F7F7
  participant "ctrl : OrdersController" as CTRL
  participant "h : CreateOrderCommandHandler" as H
end box
box "Domain services" #FAFAFA
  participant "uow : IUnitOfWork" as UOW
  participant "wallet : IWalletDomainService" as WAL
  participant "val : ICartValidationService" as VAL
end box
database "db : PostgreSQL" as DB
participant "noti : IBusinessNotificationService" as NOTI
participant "robot : Robot / Unity twin" as ROBOT

activate STU
STU -> CTRL ++: POST /api/orders(sessionId, cartVersion)
CTRL -> H ++: Send(CreateOrderCommand) via IMediator

opt [cartVersion <= 0 | sessionId is empty]
  H -->> CTRL: Failure(InvalidValue) : 400
end

critical transaction + per-user row lock — serializes concurrent checkouts
  H -> UOW ++: BeginTransactionAsync()
  deactivate UOW
  H -> WAL ++: LockUserAsync(userId)
  WAL -> DB ++: SELECT 1 FROM "Users" WHERE "Id" = @userId FOR UPDATE
  DB -->> WAL --: lock granted — blocks until the row/key is free
  deactivate WAL

  H -> DB ++: Carts.FirstOrDefaultAsync(c => c.UserId == userId)
  DB -->> H --: cart | null

  opt [cart is null]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(NullValue, "Cart was not found.") : 400
  end
  opt [cart.Version != cartVersion]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(CartVersionConflict, "Current version is {n}.") : 409
  end
  opt [cart JSON unparseable | session not present in the cart]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(InvalidValue | NullValue, reason) : 400
  end

  ref over H, VAL, DB
    ValidateCart(session, requireCompleteTemplate = true) : ValidatedCart
    session open, dishes active and in the session, template min/max/required satisfied
  end ref
  opt [validation failed]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(validation error) : 400
  end

  H -> DB ++: Users.FirstOrDefaultAsync(u => u.Id == userId)
  DB -->> H --: user -> totalPrice = sum(live dish price x quantity)
  opt [user.Balance < totalPrice]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(InvalidValue, "Insufficient balance. Required {x}, Available {y}") : 400
  end

  H -> WAL ++: TryDebitUserBalanceAsync(userId, totalPrice) : decimal?
  WAL -> DB ++: UPDATE "Users" SET "Balance_Amount" = "Balance_Amount" - @amount\nWHERE "Id" = @userId AND "Balance_Amount" >= @amount RETURNING "Balance_Amount"
  DB -->> WAL --: balanceAfter | no row
  WAL -->> H --: balanceAfter | null
  opt [debit returned null]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(InvalidValue, "Insufficient balance.") : 400
  end

  H -> H: order = Order.Create(sessionId, mealTemplateId, userId)\n+ AddDish(dishId, quantity, live unit price) per cart item\n+ AttachTransaction(walletTransaction.Id)
  H -> DB ++: orderRepository.AddAsync(order) + walletTransactionRepository.AddAsync(tx)\n+ cart.Update(...) which bumps Version in the aggregate, then SaveChangesAsync()\nEF emits the Order / OrderItem / WalletTransaction inserts and the Cart update
  DB -->> H --: rows written
  H -> UOW ++: CommitAsync()
  deactivate UOW
end

H -> NOTI ++: NotifyAsync(OrderCreated, userId, order.Id, { totalPrice })
NOTI -->> H --: queued
NOTI ->> STU: SignalR + FCM "order created"

group best-effort — a failure here is logged, never rolls the order back
  ref over H, DB, ROBOT
    CreateServingJob(order.Id)
    idempotent, queues ServingJob (Queued, TrayId = null),
    moves Order to Preparing with history, then pings the robots
  end ref
end

H -->> CTRL --: Success(CreateOrderResponse)
CTRL -->> STU --: 201 Created (Location: /api/orders/{id})
deactivate STU

@enduml
```
