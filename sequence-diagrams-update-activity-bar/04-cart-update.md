# 4 · PUT /api/cart

Corrected copy of `sd UpdateCart — PUT /api/cart` from [`../sequence-diagrams/cart-diagrams.md`](../sequence-diagrams/cart-diagrams.md).

## What changed

- DB reply added after «SELECT 1 FROM "Users" WHERE "Id" = @userId FOR UPDAT»
- DB reply added after «cartRepository.AddAsync(Cart.Create(userId, dataJson»
- DB reply added after «cart.Update(dataJson, userId) — Version++ in the agg»

```plantuml
@startuml
title sd UpdateCart — PUT /api/cart
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "student : Student" as STU
box "API" #F7F7F7
  participant "ctrl : CartController" as CTRL
  participant "h : UpdateCartCommandHandler" as H
end box
box "Domain services" #FAFAFA
  participant "val : ICartValidationService" as VAL
  participant "uow : IUnitOfWork" as UOW
  participant "wallet : IWalletDomainService" as WAL
end box
database "db : PostgreSQL" as DB

activate STU
STU -> CTRL ++: PUT /api/cart(expectedVersion, data)
CTRL -> H ++: Send(UpdateCartCommand) via IMediator

opt [expectedVersion < 0]
  H -->> CTRL: Failure(InvalidValue, "Expected version cannot be negative.") : 400
end

ref over H, VAL, DB
  ValidateCart(data, requireCompleteTemplate = false)
  no duplicate session or dish, every session and dish resolved and active,
  session open for ordering, dish belongs to the session,
  template per-category min/max and required rules
end ref
opt [validation failed]
  H -->> CTRL: Failure(validation error) : 400
end

critical transaction + per-user row lock
  H -> UOW ++: BeginTransactionAsync()
  deactivate UOW
  H -> WAL ++: LockUserAsync(userId)
  WAL -> DB ++: SELECT 1 FROM "Users" WHERE "Id" = @userId FOR UPDATE
  DB -->> WAL --: lock granted — blocks until the row/key is free
  deactivate WAL

  H -> DB ++: Carts.FirstOrDefaultAsync(c => c.UserId == userId)
  DB -->> H --: cart | null -> currentVersion = cart?.Version ?? 0

  opt [currentVersion != expectedVersion]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(CartVersionConflict, "Current version is {n}.") : 409
  end

  alt [no cart row yet]
    H -> DB ++: cartRepository.AddAsync(Cart.Create(userId, dataJson))\nVersion = 1 is set in the aggregate, SaveChangesAsync() emits the INSERT
    DB -->> H --: rows written
  else [cart exists]
    H -> DB ++: cart.Update(dataJson, userId) — Version++ in the aggregate\ncartRepository.Update(cart), SaveChangesAsync() emits\nUPDATE ... WHERE "Version" = @original (concurrency token)
    DB -->> H --: rows written
  end
  H -> UOW ++: CommitAsync()
  deactivate UOW
end

H -->> CTRL --: Success(CartResponse with the new Version)
CTRL -->> STU --: 200 OK
deactivate STU

@enduml
```
