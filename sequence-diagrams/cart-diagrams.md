# CartController — Sequence Diagrams

Base route: **`/api/cart`** · Auth: **JWT (any authenticated user)** · API version `1.0`
Source: [`CartController.cs`](../SC.Api/Controllers/CartController.cs)

| Endpoint | Handler | Response |
|---|---|---|
| `GET /api/cart` | `GetCartQueryHandler` | `200` — cart + `version` |
| `PUT /api/cart` | `UpdateCartCommandHandler` | `200` / `409` on stale version |
| `DELETE /api/cart?expectedVersion=n` | `ClearCartCommandHandler` | `200` / `409` on stale version |

The cart is **one row per user** holding the whole basket as a JSON document (`Carts.DataJson`) plus an integer `Version`. Every write is optimistic-concurrency checked against that `Version` under a PostgreSQL row lock on the owning user.

**Layers:** `CartController → IMediator → Handler → ICartValidationService / IWalletDomainService / IGenericRepository → SmartCanteenDbContext → PostgreSQL`

---

## 1. `GET /api/cart` — read with self-repair

This is **not** a plain read. Sessions that closed since the cart was written are dropped, items are re-enriched with live dish data, and if anything was dropped the handler **writes the cleaned cart back** — inside a transaction, under a user row lock, and only if the version has not moved in the meantime.

```plantuml
@startuml
title sd GetCart — GET /api/cart, read with self-repair
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "student : Student" as STU
box "API" #F7F7F7
  participant "ctrl : CartController" as CTRL
  participant "h : GetCartQueryHandler" as H
end box
box "Domain services" #FAFAFA
  participant "uow : IUnitOfWork" as UOW
  participant "wallet : IWalletDomainService" as WAL
end box
database "db : PostgreSQL" as DB

activate STU
STU -> CTRL ++: GET /api/cart
CTRL -> H ++: Send(GetCartQuery) via IMediator

H -> DB ++: Carts.FirstOrDefaultAsync(c => c.UserId == userId)
DB -->> H --: cart | null
opt [no cart row]
  H -->> CTRL: Success(empty CartData, Version = 0) : 200
end

H -> H: cartData = CartJson.Deserialize(cart.DataJson)
ref over H, DB
  RemoveExpiredSessions(cartData)
  Sessions.Where(IsActive && now between AvailableForOrder and AvailableTo)
end ref
ref over H, DB
  EnrichCartItems(cartData)
  Dishes.Where(d => dishIds.Contains(d.Id)) then refresh DishName and ImgUrl
end ref

opt [nothing was dropped]
  H -->> CTRL: Success(cartData, cart.Version) : 200
end

critical write-back under the per-user row lock
  H -> UOW ++: BeginTransactionAsync()
  deactivate UOW
  H -> WAL ++: LockUserAsync(userId)
  WAL -> DB ++: SELECT 1 FROM "Users" WHERE "Id" = @userId FOR UPDATE
  deactivate DB
  deactivate WAL
  H -> DB ++: Carts.FirstOrDefaultAsync(c => c.UserId == userId) — re-read inside the lock
  DB -->> H --: currentCart

  alt [currentCart != null and currentCart.Version == cart.Version]
    H -> DB ++: currentCart.Update(CartJson.Serialize(activeCartData), userId)\n+ cartRepository.Update(currentCart), then SaveChangesAsync()
    deactivate DB
    H -> UOW ++: CommitAsync()
    deactivate UOW
    H -> H: return the cleaned cart
  else [another writer moved the version first]
    H -> UOW ++: RollbackAsync()
    deactivate UOW
    H -> H: return the OTHER writer's cart, not the cleaned one
  end
end

H -->> CTRL --: Success(cartData, Version)
CTRL -->> STU --: 200 OK
deactivate STU

note over H, DB: any exception -> RollbackAsync() + Failure(ServerError)\nGET can therefore change the version with no user action —\nalways store the version the API just returned
@enduml
```

**Consequences for the FE**

- `GET /api/cart` can return a **different `version`** than the previous call even with no user action — always store the version the API just returned.
- A cart item can silently disappear between two reads because its session closed. Re-render from the response, never from local state.

---

## 2. `PUT /api/cart` — validated, version-checked write

Validation runs **before** the transaction opens, so an invalid basket never takes the user lock.

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
  deactivate DB
  deactivate WAL

  H -> DB ++: Carts.FirstOrDefaultAsync(c => c.UserId == userId)
  DB -->> H --: cart | null -> currentVersion = cart?.Version ?? 0

  opt [currentVersion != expectedVersion]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(CartVersionConflict, "Current version is {n}.") : 409
  end

  alt [no cart row yet]
    H -> DB ++: cartRepository.AddAsync(Cart.Create(userId, dataJson))\nVersion = 1 is set in the aggregate, SaveChangesAsync() emits the INSERT
    deactivate DB
  else [cart exists]
    H -> DB ++: cart.Update(dataJson, userId) — Version++ in the aggregate\ncartRepository.Update(cart), SaveChangesAsync() emits\nUPDATE ... WHERE "Version" = @original (concurrency token)
    deactivate DB
  end
  H -> UOW ++: CommitAsync()
  deactivate UOW
end

H -->> CTRL --: Success(CartResponse with the new Version)
CTRL -->> STU --: 200 OK
deactivate STU

@enduml
```

**Failure mapping.** `DbUpdateConcurrencyException` → rollback + `CartVersionConflict` → `409`; any other exception → rollback + `ServerError` → `400`.

**Why `requireCompleteTemplate: false`.** A cart being edited is allowed to be incomplete — the full meal-template check is deferred to checkout, where `POST /api/orders` runs the same validator with `true`.

---

## 3. `DELETE /api/cart` — clear, same concurrency contract

```plantuml
@startuml
title sd ClearCart — DELETE /api/cart
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "student : Student" as STU
box "API" #F7F7F7
  participant "ctrl : CartController" as CTRL
  participant "h : ClearCartCommandHandler" as H
end box
box "Domain services" #FAFAFA
  participant "uow : IUnitOfWork" as UOW
  participant "wallet : IWalletDomainService" as WAL
end box
database "db : PostgreSQL" as DB

activate STU
STU -> CTRL ++: DELETE /api/cart?expectedVersion=n
CTRL -> H ++: Send(ClearCartCommand(expectedVersion)) via IMediator

opt [expectedVersion < 0]
  H -->> CTRL: Failure(InvalidValue) : 400
end

critical transaction + per-user row lock
  H -> UOW ++: BeginTransactionAsync()
  deactivate UOW
  H -> WAL ++: LockUserAsync(userId)
  WAL -> DB ++: SELECT 1 FROM "Users" WHERE "Id" = @userId FOR UPDATE
  deactivate DB
  deactivate WAL

  H -> DB ++: Carts.FirstOrDefaultAsync(c => c.UserId == userId)
  DB -->> H --: cart | null

  opt [currentVersion != expectedVersion]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Failure(CartVersionConflict) : 409
  end
  opt [no cart row]
    H -> UOW: RollbackAsync()
    H -->> CTRL: Success(empty cart, Version 0) : 200
  end

  H -> DB ++: cart.Update(CartJson.Serialize(new CartData()), userId)\n+ cartRepository.Update(cart), then SaveChangesAsync()
  deactivate DB
  H -> UOW ++: CommitAsync()
  deactivate UOW
end

H -->> CTRL --: Success(empty cart, new Version)
CTRL -->> STU --: 200 OK
deactivate STU
@enduml
```

---

## Cart version state

```plantuml
@startuml
title Cart version state
hide empty description

[*] --> NoCart : user has never written a cart
NoCart --> V1 : PUT with expectedVersion 0
V1 --> Vn : PUT / DELETE with the matching version
Vn --> Vn : GET that drops a closed session (version bumps on its own)
Vn --> Conflict409 : PUT / DELETE with a stale version
Conflict409 --> Vn : FE re-reads GET /api/cart and retries
Vn --> Vn : POST /api/orders removes the checked-out session
@enduml
```

---

## Related

- [`refunds-diagrams.md`](refunds-diagrams.md) · [`changeproposals-diagrams.md`](changeproposals-diagrams.md)
- Checkout (`POST /api/orders`) consumes the cart version — see [`orders-diagrams.md`](orders-diagrams.md)
- [`API-Modules/cart-api.md`](../API-Modules/cart-api.md) — request/response reference
