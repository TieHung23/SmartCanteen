# SmartCanteen Architecture Analysis — DDD (Domain-Driven Design)

---

## 1. Architecture Overview

### 1.1. Introduction

SmartCanteen is a smart canteen management system for FPT University, with the following modules:
- **Auth & Users**: Registration, login (JWT + Google OAuth), email verification, profile management
- **Menu & Ordering**: Dish categories, meal sessions, cart, ordering
- **Payment**: Point wallet, top-ups via SePay/Momo/ZaloPay/VnPay
- **Robot Serving**: FAIRINO FR3 robot integration, tray/pickup shelf management, serving jobs
- **Notification**: Real-time notifications via SignalR + Firebase Cloud Messaging

### 1.2. Project Structure (Clean Architecture, 7 layers)

| Layer | Project | Role |
|-------|---------|---------|
| **Core/Contract** | `SC.Contract` | Shared interfaces: ICommand, IQuery, IDomainEvent, Result/Error, service contracts |
| **Domain** | `SC.Domain` | Domain entities, aggregate roots, value objects, enums, repository interfaces |
| **Application** | `SC.Application` | CQRS handlers (MediatR), validators (FluentValidation), application services |
| **Infrastructure** | `SC.Infrastructure` | Implementations: JWT, BCrypt, Cloudinary, Resend email, Firebase, Redis, SePay |
| **Persistence** | `SC.Persistence` | EF Core DbContext, migrations, repository implementations, UnitOfWork |
| **Presentation** | `SC.Api` | ASP.NET Controllers, SignalR Hubs, Middleware |
| **Test** | `SC.Architecture.Test` | Architecture validation tests |

### 1.3. Core Technologies

- **Runtime**: .NET 10, C# 13
- **Database**: PostgreSQL 16, EF Core 8.0.4 (Npgsql)
- **CQRS**: MediatR 14.1.0
- **Auth**: JWT Bearer + Google OAuth + BCrypt
- **Real-time**: SignalR
- **Payment**: SePay, Momo, ZaloPay, VnPay
- **Storage**: Cloudinary
- **Cache**: Redis
- **Logging**: Serilog (console, file, PostgreSQL)
- **API Versioning**: Asp.Versioning.Mvc (header-based)

---

## 2. Aggregate Root Inventory

| Aggregate Root | File | Description |
|---------------|------|-------|
| `User` | `SC.Domain/Domain/User/AggregateRoot/User.cs` | User (Student/Manager/Admin/Staff) |
| `Category` | `SC.Domain/Domain/Category/AggregateRoot/Category.cs` | Dish category |
| `Dish` | `SC.Domain/Domain/Dish/AggregateRoot/Dish.cs` | Dish |
| `Session` | `SC.Domain/Domain/Session/AggregateRoot/Session.cs` | Meal session |
| `Cart` | `SC.Domain/Domain/Cart/AggregateRoot/Cart.cs` | Cart (JSON blob + version) |
| `Order` | `SC.Domain/Domain/Order/AggregateRoot/Order.cs` | Order |
| `Payment` | `SC.Domain/Domain/Payment/AggregateRoot/Payment.cs` | Payment transaction |
| `WalletTransaction` | Entity (not an AggregateRoot) | Wallet transaction history |
| `Notification` | `SC.Domain/Domain/Notification/AggregateRoot/Notification.cs` | Notification |
| `RefundRequest` | `SC.Domain/Domain/Refund/AggregateRoot/RefundRequest.cs` | Refund request |
| `Setting` | `SC.Domain/Domain/Setting/AggregateRoot/Setting.cs` | System configuration |
| `ApiLog` | `SC.Domain/Domain/Logging/AggregateRoot/ApiLog.cs` | API log |
| `VerificationRequest` | `SC.Domain/Domain/Verification/AggregateRoot/VerificationRequest.cs` | Identity verification |
| `RobotArm` | `SC.Domain/Domain/RobotArm/Entity/RobotArm.cs` | Robot arm (Entity, not an AggregateRoot) |
| `ServingJob` | `SC.Domain/Domain/ServingJob/Entity/ServingJob.cs` | Serving job |
| `AdminCommandAudit` | `SC.Domain/Domain/AdminCommandAudit/Entity/AdminCommandAudit.cs` | Audit |

---

## 3. DDD Violations

### 3.1. Domain Events are defined but NEVER used (dead code)

- **Files**: `SC.Domain/Abstraction/Aggregates/AggregateRoot.cs`, `SC.Contract/Abstraction/Message/IDomainEvent.cs`
- **Problem**: `AggregateRoot<T>` has `_domainEvents`, `AddDomainEvent()`, `ClearDomainEvents()`, and the `IDomainEvent` interface is fully defined, BUT not a single line of code in the whole system calls `AddDomainEvent()`.
- **Evidence**: Grepping `AddDomainEvent` only returns the definition at `AggregateRoot.cs:17`. Every `EntityConfiguration` in Persistence has `builder.Ignore(x => x.DomainEvents)` so EF Core skips the field.
- **Consequence**: The whole domain-event mechanism is dead code. Side effects (sending email, notifications, creating serving jobs) are invoked directly from handlers instead of reacting to domain events.

### 3.2. Anemic Domain Model — business logic lives in the Application layer

- **Representative file**: `SC.Application/MediatR/Order/CreateOrder/CreateOrderCommandHandler.cs`
- **Problem**: Important business rules are implemented in the handler rather than in domain entities/aggregates:
  - Wallet balance check (`user.Balance.Amount < totalPrice`) in the handler (lines 132-138)
  - Cart version-conflict check (lines 68-74)
  - Session dish reservation (lines 140-155) — calls `unitOfWork.TryReserveSessionDishAsync`
  - Balance debit (lines 157-160) — calls `unitOfWork.TryDebitUserBalanceAsync`
  - Creating the transaction and attaching it to the order (lines 183-191)
  - Removing the checked-out session from cart data (lines 194-198)
- **DDD solution**: These rules should be encapsulated in a domain service or aggregate method. For example, `Order.Create()` should take `Cart`, `User`, `SessionDish` and validate/debit/reserve itself.

### 3.3. Repository returns IQueryable — breaks Persistence Ignorance

- **File**: `SC.Domain/Abstraction/Repositories/IGenericRepository.cs` (line 17)
- **Problem**: `IGenericRepository` exposes `IQueryable<TEntity> GetQueryable(...)`. This lets the application layer write LINQ queries directly against the database, breaking repository encapsulation.
- **Evidence**: In `RegisterUserCommandHandler.cs` (lines 32-34, 45-47), the handler calls:
  ```csharp
  userRepository.GetQueryable(u => u.Email == normalizedEmail).AnyAsync(cancellationToken);
  ```
- **DDD solution**: Repositories should only return domain objects or specifications. Specific queries should be named repository methods (e.g. `IsEmailTakenAsync(string email)`).

### 3.4. Two repository abstractions doing the same job (redundancy)

- **Files**: `IGenericRepository.cs` and `IRepositoryBase.cs`
- **Problem**: The interfaces `IGenericRepository<TEntity, TKey>` and `IRepositoryBase<TEntity, TKey>` both do CRUD but with different method names:
  - `IGenericRepository`: `GetByIdAsync`, `AddAsync`, `Update`, `Delete` + `GetQueryable`
  - `IRepositoryBase`: `FindByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` + `FindAll`, `BulkUpdatePropertyAsync`, `SoftDeleteWithConditionAsync`
- **Consequence**: With separate `GenericRepository` and `RepositoryImp` implementations, developers easily confuse them. `IRepositoryBase` also has `UpdateWithConditionAsync` and `SoftDeleteWithConditionAsync` that use reflection, which is extremely dangerous.

### 3.5. RepositoryImp calls SaveChanges() inside every CRUD operation

- **File**: `SC.Persistence/Database/Repository/RepositoryImp.cs`
- **Problem**: `AddAsync`, `UpdateAsync`, `DeleteAsync` (lines 69-131) call `_context.SaveChangesAsync()` right after each operation, breaking the Unit of Work pattern.
- **Consequence**: When a use case needs multiple operations (e.g. Add user then Add token), if adding the user succeeds (already saved) but adding the token fails, there is no way to roll back.

### 3.6. IUnitOfWork is polluted with business operations

- **File**: `SC.Domain/Abstraction/Repositories/IUnitOfWork.cs`
- **Problem**: `IUnitOfWork` contains business-specific methods such as:
  - `LockUserAsync(Guid userId)` — database row lock
  - `TryDebitUserBalanceAsync(Guid userId, decimal amount)` — conditional balance update
  - `TryCreditUserBalanceAsync(Guid userId, decimal amount)`
  - `TryReserveSessionDishAsync(Guid sessionId, Guid dishId, int quantity)`
  - `LockRefundRequestAsync(Guid refundRequestId)`
- **DDD solution**: A Unit of Work should only manage transactions (Begin/Commit/Rollback). Business-specific methods belong in domain services or repository methods.

### 3.7. Entity<T> contains an infrastructure concern (IsDeleted)

- **File**: `SC.Domain/Abstraction/Entities/Entity.cs` (line 10)
- **Problem**: `Entity<T>` has `public bool IsDeleted { get; protected set; }` on the base entity. Soft-delete is a persistence concern, not a domain concept.
- **Consequence**: Every entity is forced to have IsDeleted even when soft-delete is not needed. The `SoftDelete()` method on the Entity base class is also infrastructure logic.

### 3.8. OrderItem is declared a ValueObject but behaves like an Entity

- **File**: `SC.Domain/Domain/Order/ValueObject/OrderItem.cs`
- **Problem**: `OrderItem` inherits `ValueObject` but:
  - Has `DishId` (public setter) acting as its own identity
  - Has a navigation property `Dish Dish` (persistence concern)
  - Has `OrderId` (link to Order)
  - `Quantity` and `UnitPrice` look value-object-like (init/setter), but `DishId` has a setter
- **Contradiction**: In DDD, a Value Object must be immutable. `OrderItem` has both its own identity and mutability.

### 3.9. SessionDish is neither an Entity nor a ValueObject

- **File**: `SC.Domain/Domain/Dish/AggregateRoot/SessionDish.cs`
- **Problem**: `SessionDish` is a plain class (inherits neither Entity nor ValueObject) in the `Dish.AggregateRoot` namespace, yet it is a many-to-many relationship entity:
  ```csharp
  public class SessionDish {
      public Guid DishId { get; set; }
      public Guid SessionId { get; set; }
      public int Quantity { get; set; }
      public Dish Dish { get; set; } = null!;
      public SessionAggregateRoot Session { get; set; } = null!;
  }
  ```
- **Consequence**: The aggregate boundary is unclear — does SessionDish belong to the Dish aggregate or the Session aggregate? It has bidirectional navigation properties, violating aggregate partitioning.

### 3.10. WalletTransaction is a standalone Entity instead of part of the User aggregate

- **File**: `SC.Domain/Domain/WalletTransaction/Entity/WalletTransaction.cs`
- **Problem**: `WalletTransaction` sits in its own namespace (`WalletTransaction.Entity`) as an independent Entity, but a wallet transaction is part of the User aggregate (User has Balance; every transaction changes that balance). In DDD, WalletTransaction should be a child entity of the User aggregate root.

### 3.11. Domain contains enums/logic that are not domain concepts

- **File**: `SC.Domain/SharedKernel/Enums/AppLogLevel.cs`
- **Problem**: `AppLogLevel` (ERROR, INFO, DEBUG) is a logging (infrastructure) concern, not a domain concept.
- **Similarly**: `NotificationConstraints`, `DeviceTokenConstraints` (max lengths) are validation constraints and belong in the application layer.

### 3.12. Business logic in raw SQL via UnitOfWork instead of the Domain

- **File**: `SC.Persistence/Database/Repository/UnitOfWork.cs` (lines 69-181)
- **Problem**: `TryDebitUserBalanceAsync`, `TryCreditUserBalanceAsync`, `TryReserveSessionDishAsync` all use raw SQL directly, bypassing the entire domain model:
  ```sql
  UPDATE "Users" SET "Balance_Amount" = "Balance_Amount" - @amount
  WHERE "Id" = @userId AND "Balance_Amount" >= @amount RETURNING "Balance_Amount";
  ```
- **Consequence**: Domain logic (deducting balance, reserving a dish) is implemented in the persistence layer; the business logic cannot be tested without a real database.

### 3.13. IRepositoryBase uses reflection and string-based properties

- **File**: `SC.Persistence/Database/Repository/RepositoryImp.cs` (lines 139-280)
- **Problem**: `SoftDeleteWithConditionAsync` and `BulkUpdatePropertyAsync` use reflection with the string `flagProperty = "IsDeleted"`:
  ```csharp
  var property = typeof(TEntity).GetProperty(flagProperty);
  property.SetValue(entity, true);
  ```
- **Consequence**: Loss of type-safety; runtime failures if the property name changes or does not exist.

### 3.14. No Domain Services

- **Problem**: Complex business logic that spans multiple aggregates (e.g. checkout needs Cart + User + Session + Order + WalletTransaction + ServingJob) is not extracted into a domain service. It all lives in `CreateOrderCommandHandler`.
- **DDD solution**: There should be an `OrderingDomainService` or `CheckoutService` in the domain layer; the handler should only call that service.

### 3.15. IAuditableEntity forces public setters

- **File**: `SC.Domain/Abstraction/Entities/IAuditableEntity.cs`
- **Problem**: The interface requires `{ get; set; }` (public setters) for the audit fields `CreatedAtUtc`, `UpdatedAtUtc`, `CreatedBy`, `UpdatedBy`. Any code can therefore modify the audit fields.
- **Consequence**: Entities are forced into public setters instead of private ones, reducing encapsulation.

### 3.16. Unclear Bounded Context boundaries

- **Problem**: ServingJob (robot context) references OrderId, TrayId, RobotArmId, PickupSlotId directly without going through domain events or an anti-corruption layer. This couples the robot context to the ordering context.
- **Similarly**: The Notification context references UserId and OrderId directly.

### 3.17. OrderItem: OwnsMany with a composite key — ValueObject vs Entity contradiction

- **File**: `SC.Persistence/Database/Configuration/OrderConfiguration.cs` (lines 38-64)
- **Problem**: `OrderItem` is declared a `ValueObject` in the domain (`SC.Domain/Domain/Order/ValueObject/OrderItem.cs`) but the Persistence configuration uses:
  ```csharp
  orderItem.HasKey(x => new { x.OrderId, x.DishId });  // composite key = entity
  orderItem.HasOne(x => x.Dish).WithMany()...;           // navigation = entity
  ```
- **Contradiction**: A Value Object has NO identity of its own. If it has a composite key and a navigation property, it is an Entity, not a Value Object.

### 3.18. Entity configuration uses Data Annotations in the Domain

- **File**: `SC.Domain/Abstraction/Entities/Entity.cs` (line 5)
- **Problem**: The `[Key]` data annotation on the `Id` property of a domain entity. This is a persistence concern.
- **Solution**: Use the Fluent API in the EF Core configuration instead of data annotations in the domain.

### 3.19. Public setters on domain properties

- **Files**: Most domain entities
- **Problem**: Many entities use public `{ get; set; }` instead of `{ get; private set; }`. Examples:
  - `User.cs`: `Name`, `Email`, `PasswordHash`, `Role`, `Status` all have public setters
  - `Order.cs`: `Status` has a public setter (line 30)
  - `Dish.cs`: most properties have public setters
- **Consequence**: The domain model cannot protect its invariants — any code can set arbitrary values.

### 3.20. ApiLog and Logging in the Domain layer

- **File**: `SC.Domain/Domain/Logging/AggregateRoot/ApiLog.cs`
- **Problem**: `ApiLog` (logging HTTP request/response) is an infrastructure/cross-cutting concern, not a domain concept. Placing it in the Domain layer is wrong.

### 3.21. Cart as a JSON blob — DDD anti-pattern

- **File**: `SC.Domain/Domain/Cart/AggregateRoot/Cart.cs`
- **Problem**: The cart stores its data as a JSON string (`DataJson`) with no domain structure. Invariants cannot be enforced, the data cannot be queried, and history cannot be tracked.
- **Consequence**: Business logic must deserialize/serialize JSON to validate — very error-prone.

### 3.22. PasswordResetToken and EmailVerificationToken share the EmailVerificationToken class

- **File**: `SC.Domain/Domain/User/AggregateRoot/EmailVerificationToken.cs`
- **Problem**: The file structure has separate `EmailVerificationToken`, `PasswordResetToken`, and `RefreshToken`, but `RegisterUserCommandHandler` imports `EmailVerificationTokenAggregate` for email verification. It is unclear whether `PasswordResetToken` differs at all.

---

## 4. Strengths (DDD compliance)

### 4.1. Factory method pattern used correctly

- Most aggregate roots have static factory methods (`User.Register()`, `Order.Create()`, `Payment.Create()`) instead of public constructors.
- `Notification.Create()` has validation logic in the factory method (validates required fields, normalizes values).

### 4.2. The Money value object is well implemented

- Immutable: `Amount` and `Currency` are getter-only
- `GetEqualityComponents()` for value-based comparison
- `Money.Create()` factory method with validation (no negatives allowed)
- `Add()` returns a new Money instead of mutating

### 4.3. Private constructors + factory methods

- Many entities use a `private constructor` (required by EF Core) + a static `Create()` factory method. This pattern is correct DDD.

### 4.4. Enum-based statuses define lifecycles

- `OrderStatus` is an enum with lifecycle comments
- `ServingJobStatus`, `TrayStatus`, `RobotArmStatus` define states clearly
- Entity methods (`MarkPushed`, `Acknowledge`, `MarkOnShelf`) encapsulate state transitions

### 4.5. CQRS with MediatR

- Commands and Queries are separated (though not entirely strictly)
- Validators use FluentValidation
- The `Result` pattern is used consistently

### 4.6. Clean Architecture layer separation

- Domain references neither Persistence nor Infrastructure
- Application references only Domain and Contract
- All dependencies point inward (Inward Dependency Rule)

---

## 5. Improvement Recommendations

### 5.1. Short term (quick wins)
1. **Remove `IRepositoryBase`** — keep only `IGenericRepository`, since `RepositoryImp` calls SaveChanges with the wrong pattern
2. **Remove `IDomainEvent`/`DomainEvents`** if unused (or implement them properly)
3. **Move `AppLogLevel` and logging constants** out of the Domain
4. **Change `RepositoryImp.Add/Update/Delete`** — stop calling `SaveChanges` internally

### 5.2. Medium term
1. **Create Domain Services** — move business logic from handlers into the domain
2. **Fix IUnitOfWork** — extract business methods into domain services/repositories
3. **Implement Domain Events for real** — replace direct service calls in handlers
4. **Private setters on all domain properties**
5. **Turn the Cart from a JSON blob into a structured aggregate**

### 5.3. Long term
1. **Define clear Bounded Contexts** (Ordering, Payment, Robot, Notification)
2. **Anti-Corruption Layer** between contexts
3. **Domain-event-driven communication** between aggregates instead of direct references
4. **Event Sourcing** for WalletTransaction and Order
5. **Extract the Cart into its own aggregate with domain rules**

---

## 6. Detailed File Map

### SC.Contract (16 files)
| File | Purpose |
|------|----------|
| `Abstraction/Message/ICommand.cs` | Command interface |
| `Abstraction/Message/IQuery.cs` | Query interface |
| `Abstraction/Message/IDomainEvent.cs` | Domain event interface (dead code) |
| `Abstraction/Message/IDomainEventHandler.cs` | Domain event handler (dead code) |
| `Shared/Result.cs` | Result pattern |
| `Shared/Error.cs` | Error codes |
| `Shared/PaginatedList.cs` | Paginated response |
| `Shared/PaginationParams.cs` | Pagination params |
| `Services/Auth/I*.cs` | Auth service contracts |
| `Services/Email/IEmailSender.cs` | Email service |
| `Services/Payment/I*.cs` | Payment service contracts |
| `Services/Storage/I*.cs` | File upload contracts |
| `Services/Notification/I*.cs` | Notification contracts |
| `Services/Robot/I*.cs` | Robot contracts |
| `Services/Verification/I*.cs` | Verification contracts |

### SC.Domain (65 files)
| Directory | Files | Description |
|-----------|-------|-------|
| `Abstraction/Aggregates/` | `AggregateRoot.cs`, `ValueObject.cs` | Base classes |
| `Abstraction/Entities/` | `Entity.cs`, `IEntity.cs`, `IAuditableEntity.cs` | Entity base |
| `Abstraction/Repositories/` | `IUnitOfWork.cs`, `IGenericRepository.cs`, `IRepositoryBase.cs`, `IUserRepository.cs`, `IApiLogRepository.cs` | Repository interfaces |
| `Abstraction/Services/` | `ICurrentUserService.cs` | Current user |
| `Domain/User/` | `User.cs`, enums, tokens | User aggregate |
| `Domain/Category/` | `Category.cs` | Category aggregate |
| `Domain/Dish/` | `Dish.cs`, `SessionDish.cs` | Dish aggregate |
| `Domain/Session/` | `Session.cs`, `MealTemplate.cs`, `MealSettings.cs` | Session aggregate |
| `Domain/Order/` | `Order.cs`, `OrderItem.cs`, `OrderStatusHistory.cs` | Order aggregate |
| `Domain/Cart/` | `Cart.cs` | Cart aggregate |
| `Domain/Payment/` | `Payment.cs`, enums | Payment aggregate |
| `Domain/WalletTransaction/` | `WalletTransaction.cs` | Transaction entity |
| `Domain/Refund/` | `RefundRequest.cs`, `RefundPolicy.cs`, `RefundRequestImage.cs` | Refund aggregate |
| `Domain/Notification/` | `Notification.cs`, `UserDeviceToken.cs` | Notification aggregate |
| `Domain/RobotArm/` | `RobotArm.cs` | Robot entity |
| `Domain/ServingJob/` | `ServingJob.cs` | Serving job entity |
| `Domain/Tray/` | `Tray.cs` | Tray entity |
| `Domain/PickupSlot/` | `PickupSlot.cs` | Pickup slot entity |
| `Domain/SlotConfiguration/` | `SlotConfiguration.cs` | Slot config entity |
| `Domain/ShelfStock/` | `ShelfStock.cs` | Shelf stock entity |
| `Domain/RobotEventLog/` | `RobotEventLog.cs` | Robot event log |
| `Domain/Setting/` | `Setting.cs` | Setting aggregate |
| `Domain/Logging/` | `ApiLog.cs` | Logging (should not be here) |
| `Domain/Verification/` | `VerificationRequest.cs` | Verification aggregate |
| `Domain/AdminCommandAudit/` | `AdminCommandAudit.cs` | Admin audit |
| `SharedKernel/` | `Money.cs`, `NotificationConstraints.cs`, `DeviceTokenConstraints.cs`, enums | Shared value objects |

### SC.Application (172 files)
| Module | Handlers | Files |
|--------|----------|-------|
| `Auth/` | Register, Login, GoogleLogin, Refresh, Logout, VerifyEmail, Forgot/Reset/ChangePassword, GetCurrentUser, UpdateProfile | ~30 files |
| `Cart/` | GetCart, UpdateCart, ClearCart | ~8 files |
| `Category/` | CRUD | ~15 files |
| `Dish/` | CRUD | ~15 files |
| `Session/` | CRUD | ~15 files |
| `Order/` | CRUD + CreateOrder | ~15 files |
| `Payment/` | TopUp, HandleSePayIpn, GetPayment | ~8 files |
| `Notification/` | CRUD, DeviceToken, Admin | ~25 files |
| `Robot/` | CreateServingJob, ReportStatus | ~8 files |
| `Refund/` | Submit, Approve, Reject | ~15 files |
| `RefundPolicy/` | CRUD | ~10 files |
| `Pickup/` | AssignSlot, Collect | ~6 files |
| `Setting/` | CRUD | ~10 files |
| `Verification/` | Admin approve/reject | ~8 files |

### SC.Persistence (29 configurations + 5 repositories)
| File | Purpose |
|------|----------|
| `SmartCanteenDbContext.cs` | DbContext with 28 DbSets |
| `Repository/GenericRepository.cs` | Generic CRUD (IQueryable) |
| `Repository/RepositoryImp.cs` | Redundant CRUD (SaveChanges inside) |
| `Repository/UnitOfWork.cs` | Transactions + raw SQL operations |
| `Repository/UserRepository.cs` | User-specific queries |
| `Repository/ApiLogRepository.cs` | ApiLog cleanup |
| `Configuration/*.cs` | 29 EF Core Fluent API configs |

### SC.Api (37 files, 17 controllers)
| Controller | Routes | Auth |
|------------|--------|------|
| `AuthController` | register, login, google, refresh, logout, forgot/reset password, me | Mixed |
| `CartController` | GET/PUT/DELETE | Authorize |
| `CategoriesController` | CRUD | Authorize |
| `DishesController` | CRUD + image upload | Authorize |
| `SessionsController` | CRUD | Mixed |
| `OrdersController` | CRUD + create | Authorize |
| `PaymentsController` | top-up, sepay/ipn, get | Mixed |
| `NotificationsController` | list, read, unread, delete | Authorize |
| `DeviceTokensController` | register/unregister | Authorize |
| `RobotServingController` | create | Authorize |
| `PickupController` | assign, collect | Authorize |
| `RefundsController` | submit, list, detail | Authorize |
| `RefundPoliciesController` | list | Authorize |
| `SettingsController` | CRUD | Manager |
| `VerificationController` | CRUD | Mixed |
| `NotificationsAdminController` | create | Manager |
| `VerificationAdminController` | approve/reject | Manager |
| `RefundManagerController` | approve/reject | Manager |
| `RefundPolicyManagerController` | CRUD | Manager |

---

*Document generated on 20/06/2026 based on analysis of the SmartCanteen source code.*
