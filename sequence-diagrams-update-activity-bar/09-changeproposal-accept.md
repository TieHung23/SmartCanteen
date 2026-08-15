# 9 · POST /api/changeproposals/{id}/accept

Corrected copy of `sd AcceptChangeProposal — POST /api/changeproposals/{id}/accept` from [`../sequence-diagrams/changeproposals-diagrams.md`](../sequence-diagrams/changeproposals-diagrams.md).

## What changed

- DB reply added after «update OrderItemChangeProposals.ProposalStatus = Acc»
- NOTI now returns to H and pushes to MGR asynchronously

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
DB -->> H --: rows written
note over H, DB: no explicit transaction — SaveChangesAsync commits both updates\nin EF's implicit transaction. No money moves, so no wallet lock.

opt [the session creator is neither empty nor the caller]
  H -> NOTI ++: NotifyAsync(ChangeProposalAccepted, session.CreatedBy, proposal.Id)
  NOTI -->> H --: queued
  NOTI ->> MGR: SignalR + FCM "student swapped a dish"
end

H -->> CTRL --: Success("Dish swapped successfully.")
CTRL -->> STU --: 200 OK
deactivate STU
@enduml
```
