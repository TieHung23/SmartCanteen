# 1 · POST /api/sessions/{id}/finalize

Corrected copy of `sd FinalizeSession — POST /api/sessions/{id}/finalize` from [`../sequence-diagrams/sessions-diagrams.md`](../sequence-diagrams/sessions-diagrams.md).

## What changed

- DB reply added after «update SessionDish.PreparedQuantity,\nupdate OrderIt»
- NOTI now returns to SVC and pushes to STU asynchronously

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
  DB -->> SVC --: rows written
end

loop for each queued notification, after commit
  SVC -> NOTI ++: NotifyAsync(ChangeProposalCreated, studentId, proposalId)
  NOTI -->> SVC --: queued
  NOTI ->> STU: SignalR + FCM — SwapItem / RefundItem / RefundOrder
end

SVC -->> H --: Success("Session finalized successfully.")
H -->> CTRL --: Success
CTRL -->> MGR --: 200 OK
deactivate MGR
@enduml
```
