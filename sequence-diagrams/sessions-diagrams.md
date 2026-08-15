# SessionsController — Sequence Diagrams

Base route: **`/api/sessions`** · API version `1.0`
Source: [`SessionsController.cs`](../SC.Api/Controllers/SessionsController.cs)

| Endpoint | Auth | Handler | Diagrammed |
|---|---|---|---|
| `GET /api/sessions` | anonymous | `GetAllSessionsQueryHandler` | — paginated read |
| `GET /api/sessions/calendar` | anonymous | `GetSessionCalendarQueryHandler` | — day-by-day counts for a year |
| `GET /api/sessions/{id}` | anonymous | `GetSessionByIdQueryHandler` | — session + meal settings |
| `POST /api/sessions` | JWT | `CreateSessionCommandHandler` | [§1](#1-post-apisessions--create-a-session) |
| `PUT /api/sessions/{id}` | JWT | `UpdateSessionCommandHandler` | — see the note below |
| `DELETE /api/sessions/{id}` | JWT | `DeleteSessionCommandHandler` | — soft delete |
| `POST /api/sessions/{id}/finalize` | **`Manager`** | `FinalizeSessionCommandHandler` | [§2](#2-post-apisessionsidfinalize--manual-finalize) |
| `POST /api/sessions/{id}/finalize-now` | **`Manager`** | `FinalizeSessionNowCommandHandler` | [§2](#2-post-apisessionsidfinalize--manual-finalize) (same diagram, `startServingNow: true`) |
| *(background)* `SessionFinalizationJob` | — | `FinalizeSessionService.AutoFinalizeOverdueSessionsAsync` | [§3](#3-background--auto-finalize-at-the-deadline) |

**Layers:** `SessionsController → IMediator → Handler → IFinalizeSessionService / IGenericRepository → SmartCanteenDbContext → PostgreSQL`

> `PUT /api/sessions/{id}` is 200 LOC with 14 guarded branches (blocks editing a finalized session, shrinking a window that already has orders, template edits that would invalidate existing orders). It is flagged as a diagram candidate in [`../API-Modules/sequence-diagram-candidates.md`](../API-Modules/sequence-diagram-candidates.md) but is not drawn yet.

---

## 1. `POST /api/sessions` — create a session

A session bundles the serving window, the **meal templates** (per-category min/max/required rules a plate must satisfy) and the **dish list**. All validation is fail-fast, and the whole graph is written in one transaction.

**Request body**

```json
{
  "name": "Lunch 2026-08-11",
  "description": "Standard lunch session",
  "availableFrom": "2026-08-11T04:00:00Z",
  "availableTo": "2026-08-11T06:00:00Z",
  "availableForOrder": "2026-08-10T00:00:00Z",
  "finalizationDeadline": "2026-08-11T03:00:00Z",
  "autoFinalizePolicy": 1,
  "mealTemplates": [
    {
      "name": "Standard plate",
      "settings": [
        { "categoryId": "guid", "minQuantity": 1, "maxQuantity": 2, "isRequired": true }
      ]
    }
  ],
  "dishes": [{ "dishId": "guid" }]
}
```

**Success:** `201 Created` + `Location: /api/sessions/{id}` — every validation failure returns `400`.

```plantuml
@startuml
title sd CreateSession — POST /api/sessions
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "manager : Manager" as MGR
box "API" #F7F7F7
  participant "ctrl : SessionsController" as CTRL
  participant "h : CreateSessionCommandHandler" as H
end box
participant "session : Session" as AGG
database "db : PostgreSQL" as DB

activate MGR
MGR -> CTRL ++: POST /api/sessions(CreateSessionCommand)
CTRL -> H ++: Send(command) via IMediator

opt [name empty | AvailableFrom >= AvailableTo | AvailableForOrder > AvailableFrom\n| AutoFinalizePolicy undefined | FinalizationDeadline in the past]
  H -->> CTRL: Failure(InvalidValue, reason) : 400
end

H -> DB ++: Sessions.AnyAsync(s => !s.IsDeleted\n&& s.AvailableFrom < availableTo && availableFrom < s.AvailableTo)
DB -->> H --: hasOverlappingSession
opt [hasOverlappingSession]
  H -->> CTRL: Failure(InvalidValue, "Session time overlaps with another session.") : 400
end

H -> AGG ++: Session.Create(name, description, window, userId)
opt [FinalizationDeadline was sent]
  H -> AGG: ConfigureFinalization(deadline, autoFinalizePolicy)
end
loop for each meal template
  H -> AGG: MealTemplate.Create(name) + AddSetting(categoryId, min, max, isRequired)
end

loop for each requested dish
  H -> DB ++: Dishes.FirstOrDefaultAsync(d => d.Id == dishId)
  DB -->> H --: dish | null
  opt [dish is null | IsDeleted | !IsActive]
    H -->> CTRL: Failure(NullValue, "Dish {id} not found or inactive.") : 400
  end
  H -> AGG: AddSessionDish(SessionDish.Create(dishId))
end
deactivate AGG

critical transaction — the whole aggregate is written atomically
  H -> DB ++: BeginTransaction, then insert Session + MealTemplate\n+ MealSettings + SessionDish, then Commit
  deactivate DB
end

H -->> CTRL --: Success(CreateSessionResponse)
CTRL -->> MGR --: 201 Created (Location: /api/sessions/{id})
deactivate MGR

note over H, DB: any exception -> RollbackAsync() + Failure(ServerError) : 400
@enduml
```

The overlap predicate is half-open: `s.AvailableFrom < availableTo && availableFrom < s.AvailableTo`. A session that ends exactly when another starts is therefore allowed.

---

## 2. `POST /api/sessions/{id}/finalize` — manual finalize

The manager confirms how many portions of each dish were actually cooked. Items fully covered by the prepared quantity are **confirmed**; under-supplied items are marked `ChangePending` and get an `OrderItemChangeProposal` the student must answer.

**`/finalize-now`** is the identical handler with `startServingNow: true` — it additionally pulls `AvailableFrom` to now so already-queued serving jobs become eligible for robot pickup immediately. The two extra guards it adds are marked in the diagram.

**Request body**

```json
{
  "preparedDishes": [
    { "dishId": "guid", "preparedQuantity": 40, "suggestedDishId": "guid | null" }
  ]
}
```

**Success:** `200 OK` — all failures return `400`. Full error table: [`../API-Modules/finalize-session-now-api.md`](../API-Modules/finalize-session-now-api.md).

```plantuml
@startuml
title sd FinalizeSession — POST /api/sessions/{id}/finalize
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "manager : Manager" as MGR
box "API" #F7F7F7
  participant "ctrl : SessionsController" as CTRL
  participant "h : FinalizeSessionCommandHandler" as H
end box
participant "svc : IFinalizeSessionService" as SVC
database "db : PostgreSQL" as DB
participant "noti : IBusinessNotificationService" as NOTI
actor "student : Student" as STU

activate MGR
MGR -> CTRL ++: POST /api/sessions/{id}/finalize(preparedDishes[])
CTRL -> H ++: Send(command, SessionId = route id) via IMediator
H -> SVC ++: FinalizeAsync(sessionId, preparedDishes, managerId, startServingNow)

SVC -> DB ++: Sessions.Include(SessionDishes)\n.Include(MealTemplates).ThenInclude(Settings)\n.FirstOrDefaultAsync(s => s.Id == sessionId)
DB -->> SVC --: session | null

opt [session null | already finalized | duplicate dishIds | deadline passed]
  SVC -->> H: Failure(NullValue | InvalidValue, reason)
  H -->> CTRL: 400 Bad Request
end

opt [startServingNow — the finalize-now variant]
  SVC -> DB ++: Sessions.AnyAsync(s => s.Id != session.Id\n&& s.AvailableFrom < session.AvailableTo && now < s.AvailableTo)
  DB -->> SVC --: overlaps
  opt [AvailableTo already passed | overlaps]
    SVC -->> H: Failure(InvalidValue, reason)
    H -->> CTRL: 400 Bad Request
  end
end

loop for each prepared dish
  SVC -> SVC: dish must belong to the session\n-> sessionDish.SetPreparedQuantity(qty)
end

SVC -> DB ++: Orders.Where(o => o.SessionId == sessionId).ToListAsync()\n+ Dishes for ordered and suggested ids\n+ Settings CHANGE_PROPOSAL/RESPONSE/RESPONSE_WINDOW_MINUTES (default 30)
DB -->> SVC --: orders, dishes, proposalExpiresAtUtc

ref over SVC
  ValidateSuggestedDishes(session, orders, suggestions)
  differs from current, in the session, active, allowed by every affected
  template, same required category
end ref
opt [a suggested dish is invalid]
  SVC -->> H: Failure(InvalidValue | NullValue, reason)
  H -->> CTRL: 400 Bad Request
end

loop for each order item
  alt [preparedQuantity >= totalOrdered for that dish]
    SVC -> SVC: item.Confirm()
  else [under-supplied]
    SVC -> SVC: item.MarkChangePending()\n+ OrderItemChangeProposal.Create(..., proposalExpiresAtUtc)\n+ queue a ChangeProposalCreated notification
  end
end

critical one implicit EF transaction — items, proposals and the session commit together
  SVC -> DB ++: update SessionDish.PreparedQuantity,\nupdate OrderItem.ItemStatus = Confirmed | ChangePending,\ninsert OrderItemChangeProposals,\nupdate Session.IsFinalized (+ AvailableFrom on finalize-now)
  deactivate DB
end

loop for each queued notification, after commit
  SVC -> NOTI ++: NotifyAsync(ChangeProposalCreated, studentId, proposalId)
  NOTI -->> STU --: SignalR + FCM — SwapItem / RefundItem / RefundOrder
end

SVC -->> H --: Success("Session finalized successfully.")
H -->> CTRL --: Success
CTRL -->> MGR --: 200 OK
deactivate MGR
@enduml
```

The `finalize-now` overlap guard checks the **widened** window `[now, AvailableTo]` against every other session's *full* `[AvailableFrom, AvailableTo]` — `x.Id != session.Id && x.AvailableFrom < session.AvailableTo && now < x.AvailableTo`. Checking full windows rather than "who is active right now" also catches a session that has not started yet but would start inside the widened window.

Each proposal raised here is answered in [`changeproposals-diagrams.md`](changeproposals-diagrams.md).

---

## 3. Background — auto-finalize at the deadline

If the manager never finalizes, `SessionFinalizationJob` applies the session's `AutoFinalizePolicy` once `FinalizationDeadline` passes. **AutoReject** cancels every order and auto-credits a full refund; **AutoConfirmAll** treats the ordered quantity as the prepared quantity and confirms everything.

```plantuml
@startuml
title sd AutoFinalizeOverdueSessions — SessionFinalizationJob
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

participant "job : SessionFinalizationJob" as JOB
participant "svc : FinalizeSessionService" as SVC
participant "credit : IRefundAutoCreditService" as CREDIT
database "db : PostgreSQL" as DB
participant "noti : IBusinessNotificationService" as NOTI
actor "student : Student" as STU

JOB -> SVC ++: AutoFinalizeOverdueSessionsAsync()
SVC -> DB ++: Sessions.Where(s => !s.IsFinalized\n&& s.FinalizationDeadline <= now).Include(SessionDishes).ToListAsync()
DB -->> SVC --: overdue sessions

loop for each overdue session
  SVC -> DB ++: Orders.Where(o => o.SessionId == session.Id).ToListAsync()
  DB -->> SVC --: orders

  alt [AutoFinalizePolicy == AutoReject]
    loop for each order — its own transaction
      critical per-order transaction + advisory lock
        SVC -> DB ++: BeginTransactionAsync(), then IRefundLockService.LockOrderRefundRequestsAsync\nraw SQL: SELECT pg_advisory_xact_lock(hashtext('refund-order-{orderId}'))
        deactivate DB
        SVC -> SVC: RefundItem() every Pending / ChangePending item\n+ order.UpdateStatus(Cancelled)
        SVC -> DB ++: insert OrderStatusHistory(from -> Cancelled, "AutoFinalizeReject")
        deactivate DB
        opt [order was paid and has no active refund and amount > 0]
          SVC -> DB ++: insert RefundRequest.Submit(configured policy, full amount)
          deactivate DB
          ref over SVC, CREDIT, DB
            CreditRefund(refundRequest)
            lock user FOR UPDATE, credit balance, insert WalletTransaction
          end ref
        end
        SVC -> DB ++: SaveChanges + Commit
        deactivate DB
      end
      SVC -> SVC: queue SessionAutoRejected notification
    end
  else [AutoFinalizePolicy == AutoConfirmAll]
    SVC -> SVC: preparedQuantity = totalOrdered for every session dish\n+ item.Confirm() for every Pending item\n+ queue SessionAutoConfirmed per order
  end

  SVC -> SVC: session.AutoFinalize()
end

SVC -> DB ++: SaveChangesAsync() — session flags and AutoConfirmAll updates
deactivate DB
loop for each queued notification
  SVC -> NOTI ++: NotifyAsync(...)
  NOTI -->> STU --: SignalR + FCM
end
deactivate SVC

note over SVC: a failure on one session is logged and skipped — the sweep continues
@enduml
```

---

## Session state

```plantuml
@startuml
title Session state
hide empty description

[*] --> Draft : POST /api/sessions
Draft --> OpenForOrders : now reaches AvailableForOrder
OpenForOrders --> Finalized : POST /finalize — manager confirms prepared quantities
OpenForOrders --> Finalized : POST /finalize-now — same, plus AvailableFrom pulled to now
OpenForOrders --> AutoFinalized : deadline passes, background job applies AutoFinalizePolicy
Finalized --> Serving : now within AvailableFrom..AvailableTo, robots may pull jobs
AutoFinalized --> Serving : AutoConfirmAll
AutoFinalized --> [*] : AutoReject — every order cancelled and refunded
Serving --> Closed : now passes AvailableTo
Closed --> [*]
@enduml
```

---

## Related

- [`orders-diagrams.md`](orders-diagrams.md) — the orders this session finalizes
- [`changeproposals-diagrams.md`](changeproposals-diagrams.md) — what happens to the proposals raised in §2
- [`../API-Modules/session-api.md`](../API-Modules/session-api.md) · [`../API-Modules/finalize-session-now-api.md`](../API-Modules/finalize-session-now-api.md)
