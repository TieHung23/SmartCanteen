# 2 · POST /api/sessions

Corrected copy of `sd CreateSession — POST /api/sessions` from [`../sequence-diagrams/sessions-diagrams.md`](../sequence-diagrams/sessions-diagrams.md).

## What changed

- DB reply added after «BeginTransaction, then insert Session + MealTemplate»

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
  DB -->> H --: committed
end

H -->> CTRL --: Success(CreateSessionResponse)
CTRL -->> MGR --: 201 Created (Location: /api/sessions/{id})
deactivate MGR

note over H, DB: any exception -> RollbackAsync() + Failure(ServerError) : 400
@enduml
```
