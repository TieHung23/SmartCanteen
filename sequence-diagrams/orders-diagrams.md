# OrdersController — Sequence Diagrams

Base route: **`/api/orders`** · Auth: **JWT** · API version `1.0`
Source: [`OrdersController.cs`](../SC.Api/Controllers/OrdersController.cs)

| Endpoint | Handler | Notes |
|---|---|---|
| `POST /api/orders` | `CreateOrderCommandHandler` | checkout — the only complex path here |
| `GET /api/orders` | `GetAllOrdersQueryHandler` | non-`Manager`/`Staff` callers are forced to their own orders |
| `GET /api/orders/{id}` | `GetOrderByIdQueryHandler` | order + items + status history |
| `DELETE /api/orders/{id}` | `DeleteOrderCommandHandler` | soft delete |

Related controller: `OrderManagerController` (`/api/manager/orders`, roles `Manager,Staff`) exposes read-by-session and `PUT /{id}/status`, which **refuses** to set serving-flow statuses so it cannot collide with the robot pipeline.

**Layers:** `OrdersController → IMediator → CreateOrderCommandHandler → ICartValidationService / IWalletDomainService / IGenericRepository → SmartCanteenDbContext → PostgreSQL`

---

## 1. `POST /api/orders` — checkout one session from the cart

Checkout takes **one session** out of the server-side cart: validate the plate against the session's meal template, debit the wallet, write the order, remove that session from the cart — all in one transaction — then queue a robot serving job as best-effort work outside it.

**Request:** `{ "sessionId": "guid", "cartVersion": 7 }` — `cartVersion` is optimistic concurrency; a stale value returns `409` and the FE must reload the cart.

**Success:** `201 Created` + `{ id, transactionId, totalPrice, userRemainingBalance, cartVersion }`.

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
  deactivate DB
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
  deactivate DB
  H -> UOW ++: CommitAsync()
  deactivate UOW
end

H -> NOTI ++: NotifyAsync(OrderCreated, userId, order.Id, { totalPrice })
NOTI -->> STU --: SignalR + FCM "order created"

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

**Failure mapping.** `DbUpdateConcurrencyException` → rollback + `CartVersionConflict` → `409`; any other exception → rollback + `ServerError` → `400`.

**Why the debit cannot go negative.** `TryDebitUserBalanceAsync` guards its UPDATE with `AND "Balance_Amount" >= @amount` and returns the new balance via `RETURNING`. If two checkouts race past the earlier balance check, the second one matches no row and comes back `null` — that is the real defence, not the `user.Balance < totalPrice` comparison above it.

Note the tray is **not** assigned at this point — the serving job is queued with `TrayId = NULL` and a tray is bound later when the edge controller scans a physical tray.

---

## 2. `GET /api/orders` and `GET /api/orders/{id}` — read paths

The controller enforces scoping before the handler runs: a caller who is neither `Manager` nor `Staff` has `request.UserId` reset to `null`, so the handler falls back to the authenticated user and cannot read someone else's orders by passing a `userId` query parameter.

```plantuml
@startuml
title sd ReadOrders — GET /api/orders and GET /api/orders/{id}
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "caller : Student or Manager" as STU
box "API" #F7F7F7
  participant "ctrl : OrdersController" as CTRL
  participant "h : GetAllOrders / GetOrderById handler" as H
end box
database "db : PostgreSQL" as DB

alt [list]
activate STU
  STU -> CTRL ++: GET /api/orders?userId=...&pageNumber=1
  CTRL -> CTRL: if the caller is neither Manager nor Staff -> request.UserId = null
  note right of CTRL: scoping is enforced in the controller, so a student\ncannot read another user's orders via the query string
  CTRL -> H ++: Send(GetAllOrdersQuery) via IMediator
  H -> DB ++: Orders.Include(OrderItems).Where(o => o.CreatedBy == userId)\n.OrderByDescending(o => o.CreatedAtUtc).Skip(...).Take(...).ToListAsync()\n+ CountAsync()
  DB -->> H --: page + total
  H -->> CTRL --: Success(PaginatedList)
  CTRL -->> STU --: 200 OK
else [detail]
  STU -> CTRL ++: GET /api/orders/{id}
  CTRL -> H ++: Send(GetOrderByIdQuery(id)) via IMediator
  H -> DB ++: Orders.Include(OrderItems).FirstOrDefaultAsync(o => o.Id == id)\n+ OrderStatusHistories.Where(h => h.OrderId == id).OrderBy(h => h.CreatedAtUtc)
  DB -->> H --: order + items + history | null
  opt [not found]
    H -->> CTRL: Failure(NullValue) : 404
  end
  H -->> CTRL --: Success(detail)
  CTRL -->> STU --: 200 OK
deactivate STU
end
@enduml
```

---

## Order state

`OrderStatus` values (`SC.Domain/Domain/Order/Enum/OrderStatus.cs`): `Pending = 0`, `ReadyForPickup = 1`, `Completed = 2`, `Cancelled = 3`, `Preparing = 4`, `Expired = 7`.

```plantuml
@startuml
title Order state
hide empty description

[*] --> Pending : POST /api/orders
Pending --> Preparing : CreateServingJob, or the robot reports PickStarted
Preparing --> Preparing : session finalize → items Confirmed or ChangePending
Preparing --> Cancelled : student requests a full order refund from a proposal
Preparing --> Cancelled : auto-reject at the finalization deadline (refund credited)
Cancelled --> Preparing : manager rejects that proposal-linked order refund
Preparing --> ReadyForPickup : AssignPickupSlot — tray shelved in a slot
ReadyForPickup --> Completed : CollectOrder — student picks it up
ReadyForPickup --> Expired : ForceClearPickupSlot — no-show, staff clears the slot
Cancelled --> [*]
Completed --> [*]
Expired --> [*]
@enduml
```

The `Preparing → ReadyForPickup → Completed | Expired` leg belongs to the robot serving and pickup controllers, which are not diagrammed yet — see [`../API-Modules/sequence-diagram-candidates.md`](../API-Modules/sequence-diagram-candidates.md).

---

## Related

- [`cart-diagrams.md`](cart-diagrams.md) — where the `cartVersion` comes from
- [`sessions-diagrams.md`](sessions-diagrams.md) — finalize, which decides each item's fate
- [`changeproposals-diagrams.md`](changeproposals-diagrams.md) — the `Preparing → Cancelled` edge
- [`payments-diagrams.md`](payments-diagrams.md) — how the balance got there
- [`../API-Modules/order-api.md`](../API-Modules/order-api.md) — request/response reference
