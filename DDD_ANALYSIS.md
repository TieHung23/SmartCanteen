# Tài liệu Phân tích Kiến trúc SmartCanteen theo DDD (Domain-Driven Design)

---

## 1. Tổng quan kiến trúc

### 1.1. Giới thiệu

SmartCanteen là hệ thống quản lý căng tin thông minh cho FPT University, với các module:
- **Auth & Users**: Đăng ký, đăng nhập (JWT + Google OAuth), xác thực email, quản lý hồ sơ
- **Menu & Ordering**: Danh mục món ăn, phiên ăn (session), giỏ hàng, đặt hàng
- **Payment**: Ví điểm (Point), nạp tiền qua SePay/Momo/ZaloPay/VnPay
- **Robot Serving**: Tích hợp robot FAIRINO FR3, quản lý khay/kệ pickup, job phục vụ
- **Notification**: Thông báo real-time qua SignalR + Firebase Cloud Messaging

### 1.2. Cấu trúc Project (Clean Architecture 7 layers)

| Layer | Project | Vai trò |
|-------|---------|---------|
| **Core/Contract** | `SC.Contract` | Interfaces chung: ICommand, IQuery, IDomainEvent, Result/Error, service contracts |
| **Domain** | `SC.Domain` | Domain entities, aggregate roots, value objects, enums, repository interfaces |
| **Application** | `SC.Application` | CQRS handlers (MediatR), validators (FluentValidation), application services |
| **Infrastructure** | `SC.Infrastructure` | Implementations: JWT, BCrypt, Cloudinary, Resend email, Firebase, Redis, SePay |
| **Persistence** | `SC.Persistence` | EF Core DbContext, migrations, repository implementations, UnitOfWork |
| **Presentation** | `SC.Api` | ASP.NET Controllers, SignalR Hubs, Middleware |
| **Test** | `SC.Architecture.Test` | Architecture validation tests |

### 1.3. Công nghệ chính

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

## 2. Danh sách Aggregate Root

| Aggregate Root | File | Mô tả |
|---------------|------|-------|
| `User` | `SC.Domain/Domain/User/AggregateRoot/User.cs` | Người dùng (Student/Manager/Admin/Staff) |
| `Category` | `SC.Domain/Domain/Category/AggregateRoot/Category.cs` | Danh mục món ăn |
| `Dish` | `SC.Domain/Domain/Dish/AggregateRoot/Dish.cs` | Món ăn |
| `Session` | `SC.Domain/Domain/Session/AggregateRoot/Session.cs` | Phiên ăn (meal session) |
| `Cart` | `SC.Domain/Domain/Cart/AggregateRoot/Cart.cs` | Giỏ hàng (JSON blob + version) |
| `Order` | `SC.Domain/Domain/Order/AggregateRoot/Order.cs` | Đơn hàng |
| `Payment` | `SC.Domain/Domain/Payment/AggregateRoot/Payment.cs` | Giao dịch thanh toán |
| `WalletTransaction` | Entity (không phải AggregateRoot) | Lịch sử giao dịch ví |
| `Notification` | `SC.Domain/Domain/Notification/AggregateRoot/Notification.cs` | Thông báo |
| `RefundRequest` | `SC.Domain/Domain/Refund/AggregateRoot/RefundRequest.cs` | Yêu cầu hoàn tiền |
| `Setting` | `SC.Domain/Domain/Setting/AggregateRoot/Setting.cs` | Cấu hình hệ thống |
| `ApiLog` | `SC.Domain/Domain/Logging/AggregateRoot/ApiLog.cs` | Log API |
| `VerificationRequest` | `SC.Domain/Domain/Verification/AggregateRoot/VerificationRequest.cs` | Xác thực danh tính |
| `RobotArm` | `SC.Domain/Domain/RobotArm/Entity/RobotArm.cs` | Robot arm (Entity, không phải AggregateRoot) |
| `ServingJob` | `SC.Domain/Domain/ServingJob/Entity/ServingJob.cs` | Job phục vụ |
| `AdminCommandAudit` | `SC.Domain/Domain/AdminCommandAudit/Entity/AdminCommandAudit.cs` | Audit |

---

## 3. Các vi phạm DDD (Domain-Driven Design)

### 3.1. Domain Events được định nghĩa nhưng KHÔNG BAO GIỜ được sử dụng (Dead Code)

- **File**: `SC.Domain/Abstraction/Aggregates/AggregateRoot.cs`, `SC.Contract/Abstraction/Message/IDomainEvent.cs`
- **Vấn đề**: `AggregateRoot<T>` có `_domainEvents`, `AddDomainEvent()`, `ClearDomainEvents()`, và `IDomainEvent` interface được định nghĩa đầy đủ, NHƯNG không một dòng code nào trong toàn bộ hệ thống gọi `AddDomainEvent()`.
- **Bằng chứng**: Grep `AddDomainEvent` chỉ trả về định nghĩa ở `AggregateRoot.cs:17`. Mọi `EntityConfiguration` trong Persistence đều có `builder.Ignore(x => x.DomainEvents)` để EF Core bỏ qua trường này.
- **Hậu quả**: Toàn bộ cơ chế domain event là dead code. Các side effects (gửi email, thông báo, tạo serving job) được gọi trực tiếp từ handler thay vì phản ứng với domain events.

### 3.2. Anemic Domain Model - Business Logic nằm trong Application Layer

- **File điển hình**: `SC.Application/MediatR/Order/CreateOrder/CreateOrderCommandHandler.cs`
- **Vấn đề**: Các business rules quan trọng được implement trong handler thay vì trong domain entity/aggregate:
  - Kiểm tra số dư ví (`user.Balance.Amount < totalPrice`) ở handler (dòng 132-138)
  - Kiểm tra version conflict của cart (dòng 68-74)
  - Reserve session dish (dòng 140-155) - gọi `unitOfWork.TryReserveSessionDishAsync`
  - Debit balance (dòng 157-160) - gọi `unitOfWork.TryDebitUserBalanceAsync`
  - Tạo transaction và gán vào order (dòng 183-191)
  - Xóa session đã checkout khỏi cart data (dòng 194-198)
- **Giải pháp DDD**: Các rule này nên được đóng gói trong domain service hoặc aggregate method. Ví dụ: `Order.Create()` nên nhận `Cart`, `User`, `SessionDish` và tự validate/debit/reserve.

### 3.3. Repository trả về IQueryable - phá vỡ Persistence Ignorance

- **File**: `SC.Domain/Abstraction/Repositories/IGenericRepository.cs` (dòng 17)
- **Vấn đề**: `IGenericRepository` có method `IQueryable<TEntity> GetQueryable(...)` trả về IQueryable. Điều này cho phép application layer viết LINQ queries trực tiếp lên database, phá vỡ tính đóng gói của repository.
- **Dẫn chứng**: Trong `RegisterUserCommandHandler.cs` (dòng 32-34, 45-47), handler gọi:
  ```csharp
  userRepository.GetQueryable(u => u.Email == normalizedEmail).AnyAsync(cancellationToken);
  ```
- **Giải pháp DDD**: Repository chỉ nên trả về domain objects hoặc specifications. Các queries cụ thể nên là named methods trên repository (vd: `IsEmailTakenAsync(string email)`).

### 3.4. Hai Repository Abstractions làm cùng một việc (Redundancy)

- **File**: `IGenericRepository.cs` và `IRepositoryBase.cs`
- **Vấn đề**: Hai interface `IGenericRepository<TEntity, TKey>` và `IRepositoryBase<TEntity, TKey>` cùng làm việc CRUD nhưng khác tên method:
  - `IGenericRepository`: `GetByIdAsync`, `AddAsync`, `Update`, `Delete` + `GetQueryable`
  - `IRepositoryBase`: `FindByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` + `FindAll`, `BulkUpdatePropertyAsync`, `SoftDeleteWithConditionAsync`
- **Hậu quả**: Khi có implement `GenericRepository` và `RepositoryImp` riêng biệt, developer dễ bị nhầm lẫn. `IRepositoryBase` còn có `UpdateWithConditionAsync`, `SoftDeleteWithConditionAsync` dùng reflection, cực kỳ nguy hiểm.

### 3.5. RepositoryImp gọi SaveChanges() bên trong mỗi CRUD operation

- **File**: `SC.Persistence/Database/Repository/RepositoryImp.cs`
- **Vấn đề**: Các method `AddAsync`, `UpdateAsync`, `DeleteAsync` (dòng 69-131) gọi `_context.SaveChangesAsync()` ngay sau mỗi thao tác, phá vỡ Unit of Work pattern.
- **Hậu quả**: Khi một use case cần nhiều thao tác (vd: vừa Add user vừa Add token), nếu Add user thành công (đã SaveChanges) nhưng Add token thất bại, sẽ không thể rollback.

### 3.6. IUnitOfWork bị ô nhiễm bởi Business Operations

- **File**: `SC.Domain/Abstraction/Repositories/IUnitOfWork.cs`
- **Vấn đề**: `IUnitOfWork` chứa các method business-specific như:
  - `LockUserAsync(Guid userId)` - database row lock
  - `TryDebitUserBalanceAsync(Guid userId, decimal amount)` - update balance với điều kiện
  - `TryCreditUserBalanceAsync(Guid userId, decimal amount)`
  - `TryReserveSessionDishAsync(Guid sessionId, Guid dishId, int quantity)`
  - `LockRefundRequestAsync(Guid refundRequestId)`
- **Giải pháp DDD**: Unit of Work chỉ nên quản lý transaction (Begin/Commit/Rollback). Các method business-specific nên thuộc về domain services hoặc repository methods.

### 3.7. Entity<T> chứa Infrastructure Concern (IsDeleted)

- **File**: `SC.Domain/Abstraction/Entities/Entity.cs` (dòng 10)
- **Vấn đề**: `Entity<T>` có `public bool IsDeleted { get; protected set; }` ở base entity. Soft-delete là persistence concern, không phải domain concept.
- **Hậu quả**: Tất cả entity đều bị bắt buộc phải có IsDeleted, dù không cần soft-delete. Method `SoftDelete()` ở Entity base class cũng là infrastructure logic.

### 8. OrderItem định nghĩa là ValueObject nhưng hành xử như Entity

- **File**: `SC.Domain/Domain/Order/ValueObject/OrderItem.cs`
- **Vấn đề**: `OrderItem` kế thừa `ValueObject` nhưng:
  - Có `DishId` (setter public) làm identity riêng
  - Có navigation property `Dish Dish` (persistence concern)
  - Có `OrderId` (liên kết với Order)
  - `Quantity` và `UnitPrice` có vẻ value-object-like (init/setter), nhưng `DishId` có setter
- **Mâu thuẫn**: Trong DDD, Value Object phải immutable. `OrderItem` vừa có identity riêng vừa mutable.

### 3.9. SessionDish không phải Entity cũng không phải ValueObject

- **File**: `SC.Domain/Domain/Dish/AggregateRoot/SessionDish.cs`
- **Vấn đề**: `SessionDish` là một lớp plain (không kế thừa Entity, không kế thừa ValueObject) nằm trong namespace `Dish.AggregateRoot` nhưng lại là many-to-many relationship entity:
  ```csharp
  public class SessionDish {
      public Guid DishId { get; set; }
      public Guid SessionId { get; set; }
      public int Quantity { get; set; }
      public Dish Dish { get; set; } = null!;
      public SessionAggregateRoot Session { get; set; } = null!;
  }
  ```
- **Hậu quả**: Không rõ aggregate boundary - SessionDish thuộc về Dish aggregate hay Session aggregate? Nó có navigation property hai chiều, vi phạm nguyên tắc aggregate partition.

### 3.10. WalletTransaction là Entity riêng thay vì thuộc về User aggregate

- **File**: `SC.Domain/Domain/WalletTransaction/Entity/WalletTransaction.cs`
- **Vấn đề**: `WalletTransaction` được đặt riêng namespace (`WalletTransaction.Entity`) và là Entity độc lập, nhưng wallet transaction là một phần của User aggregate (User có Balance, mỗi transaction làm thay đổi balance). Trong DDD, WalletTransaction nên là entity con của User aggregate root.

### 3.11. Domain chứa các enum/logic không phải domain concept

- **File**: `SC.Domain/SharedKernel/Enums/AppLogLevel.cs`
- **Vấn đề**: `AppLogLevel` (ERROR, INFO, DEBUG) là logging concern (infrastructure), không phải domain concept.
- **Tương tự**: `NotificationConstraints`, `DeviceTokenConstraints` (max length) là validation constraints, nên thuộc về application layer.

### 3.12. Business Logic trong Raw SQL qua UnitOfWork thay vì Domain

- **File**: `SC.Persistence/Database/Repository/UnitOfWork.cs` (dòng 69-181)
- **Vấn đề**: `TryDebitUserBalanceAsync`, `TryCreditUserBalanceAsync`, `TryReserveSessionDishAsync` đều dùng raw SQL trực tiếp, bypass toàn bộ domain model:
  ```sql
  UPDATE "Users" SET "Balance_Amount" = "Balance_Amount" - @amount
  WHERE "Id" = @userId AND "Balance_Amount" >= @amount RETURNING "Balance_Amount";
  ```
- **Hậu quả**: Domain logic (deduct balance, reserve dish) được implement ở persistence layer, không thể test business logic mà không có database thật.

### 3.13. IRepositoryBase dùng Reflection và String-based Properties

- **File**: `SC.Persistence/Database/Repository/RepositoryImp.cs` (dòng 139-280)
- **Vấn đề**: `SoftDeleteWithConditionAsync` và `BulkUpdatePropertyAsync` dùng reflection với string `flagProperty = "IsDeleted"`:
  ```csharp
  var property = typeof(TEntity).GetProperty(flagProperty);
  property.SetValue(entity, true);
  ```
- **Hậu quả**: Mất type-safety, dễ gây lỗi runtime nếu property name đổi hoặc không tồn tại.

### 3.14. Không có Domain Services

- **Vấn đề**: Các business logic phức tạp liên quan đến nhiều aggregate (vd: checkout cần Cart + User + Session + Order + WalletTransaction + ServingJob) không được tách thành domain service. Tất cả nằm trong `CreateOrderCommandHandler`.
- **Giải pháp DDD**: Nên có `OrderingDomainService` hoặc `CheckoutService` ở domain layer, handler chỉ gọi service này.

### 3.15. IAuditableEntity buộc public setters

- **File**: `SC.Domain/Abstraction/Entities/IAuditableEntity.cs`
- **Vấn đề**: Interface yêu cầu `{ get; set; }` (public setters) cho các trường audit `CreatedAtUtc`, `UpdatedAtUtc`, `CreatedBy`, `UpdatedBy`. Điều này cho phép bất kỳ code nào cũng có thể sửa audit fields.
- **Hậu quả**: Các entity buộc phải có public setters thay vì private, làm giảm tính đóng gói.

### 3.16. Bounded Context Boundaries không rõ ràng

- **Vấn đề**: ServingJob (robot context) reference trực tiếp OrderId, TrayId, RobotArmId, PickupSlotId mà không qua domain events hay anti-corruption layer. Điều này tạo coupling giữa robot context và ordering context.
- **Tương tự**: Notification context reference UserId, OrderId trực tiếp.

### 3.17. OrderItem: OwnsMany với composite key - mâu thuẫn ValueObject vs Entity

- **File**: `SC.Persistence/Database/Configuration/OrderConfiguration.cs` (dòng 38-64)
- **Vấn đề**: `OrderItem` được khai báo là `ValueObject` trong domain (`SC.Domain/Domain/Order/ValueObject/OrderItem.cs`) nhưng configuration trong Persistence lại dùng:
  ```csharp
  orderItem.HasKey(x => new { x.OrderId, x.DishId });  // composite key = entity
  orderItem.HasOne(x => x.Dish).WithMany()...;           // navigation = entity
  ```
- **Mâu thuẫn**: Value Object KHÔNG có identity riêng. Nếu nó có composite key và navigation property, nó là Entity chứ không phải Value Object.

### 3.18. Entity Configuration dùng Data Annotations trong Domain

- **File**: `SC.Domain/Abstraction/Entities/Entity.cs` (dòng 5)
- **Vấn đề**: `[Key]` data annotation trên property `Id` trong domain entity. Đây là persistence concern.
- **Giải pháp**: Nên dùng Fluent API trong EF Core configuration thay vì data annotations trong domain.

### 3.19. Public setters trên Domain properties

- **File**: Hầu hết domain entities
- **Vấn đề**: Nhiều entities dùng `{ get; set; }` public thay vì `{ get; private set; }`. Ví dụ:
  - `User.cs`: `Name`, `Email`, `PasswordHash`, `Role`, `Status` đều là public set
  - `Order.cs`: `Status` public set (dòng 30)
  - `Dish.cs`: Hầu hết properties public set
- **Hậu quả**: Domain model không thể bảo vệ invariants - bất kỳ code nào cũng có thể set giá trị tùy ý.

### 3.20. ApiLog và Logging trong Domain Layer

- **File**: `SC.Domain/Domain/Logging/AggregateRoot/ApiLog.cs`
- **Vấn đề**: `ApiLog` (logging HTTP request/response) là infrastructure/cross-cutting concern, không phải domain concept. Đặt nó trong Domain layer là sai.

### 3.21. Cart là JSON Blob - Anti-pattern DDD

- **File**: `SC.Domain/Domain/Cart/AggregateRoot/Cart.cs`
- **Vấn đề**: Cart lưu dữ liệu dưới dạng JSON string (`DataJson`), không có cấu trúc domain. Không thể enforce invariants, không thể query, và không thể track history.
- **Hậu quả**: Business logic phải deserialize/serialize JSON để validate, rất dễ gây lỗi.

### 3.22. PasswordResetToken và EmailVerificationToken dùng chung EmailVerificationToken class

- **File**: `SC.Domain/Domain/User/AggregateRoot/EmailVerificationToken.cs`
- **Vấn đề**: Theo file structure có `EmailVerificationToken`, `PasswordResetToken`, `RefreshToken` riêng, nhưng trong `RegisterUserCommandHandler` import `EmailVerificationTokenAggregate` cho cả verify email. Không rõ `PasswordResetToken` có khác biệt gì không.

---

## 4. Điểm tốt (DDD compliance)

### 4.1. Factory methods pattern được dùng đúng

- Hầu hết aggregate roots đều có static factory method (`User.Register()`, `Order.Create()`, `Payment.Create()`) thay vì constructor public.
- `Notification.Create()` có validation logic trong factory method (validate required fields, normalize values).

### 4.2. Value Object Money được implement tốt

- Immutable: `Amount` và `Currency` chỉ có getter
- `GetEqualityComponents()` để so sánh theo value
- Factory method `Money.Create()` với validation (không cho negative)
- `Add()` method trả về Money mới thay vì mutate

### 4.3. Private constructors + Factory methods

- Nhiều entities dùng `private constructor` (EF Core required) + static `Create()` factory method. Pattern này đúng DDD.

### 4.4. Enum-based Status định nghĩa lifecycle

- `OrderStatus` có enum với lifecycle comments
- `ServingJobStatus`, `TrayStatus`, `RobotArmStatus` định nghĩa trạng thái rõ ràng
- Các method trên entity (`MarkPushed`, `Acknowledge`, `MarkOnShelf`) giúp encapsulate state transition

### 4.5. CQRS với MediatR

- Tách biệt Command và Query (dù không hoàn toàn triệt để)
- Validators dùng FluentValidation
- `Result` pattern được dùng nhất quán

### 4.6. Clean Architecture layer separation

- Domain không reference Persistence hay Infrastructure
- Application chỉ reference Domain và Contract
- Dependencies đều hướng vào trong (Inward Dependency Rule)

---

## 5. Khuyến nghị cải thiện

### 5.1. Ngắn hạn (Quick wins)
1. **Xóa bỏ `IRepositoryBase`** - chỉ giữ `IGenericRepository` vì `RepositoryImp` gọi SaveChanges sai pattern
2. **Xóa bỏ `IDomainEvent`/`DomainEvents`** nếu chưa dùng (hoặc implement đúng)
3. **Chuyển `AppLogLevel`, logging constants** ra khỏi Domain
4. **Đổi `RepositoryImp.Add/Update/Delete`** - không gọi `SaveChanges` bên trong

### 5.2. Trung hạn
1. **Tạo Domain Services** - chuyển business logic từ handler vào domain
2. **Fix IUnitOfWork** - tách business methods ra domain services/repositories
3. **Implement Domain Events thực tế** - thay thế direct service calls trong handler
4. **Private setters cho tất cả domain properties**
5. **Chuyển Cart từ JSON blob thành structured aggregate**

### 5.3. Dài hạn
1. **Định nghĩa Bounded Context rõ ràng** (Ordering, Payment, Robot, Notification)
2. **Anti-Corruption Layer** giữa các context
3. **Domain Event-driven communication** giữa các aggregate thay vì direct reference
4. **Event Sourcing** cho WalletTransaction và Order
5. **Tách Cart thành aggregate riêng với domain rules**

---

## 6. File map chi tiết

### SC.Contract (16 files)
| File | Mục đích |
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
| Directory | Files | Mô tả |
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
| File | Mục đích |
|------|----------|
| `SmartCanteenDbContext.cs` | DbContext với 28 DbSet |
| `Repository/GenericRepository.cs` | Generic CRUD (IQueryable) |
| `Repository/RepositoryImp.cs` | Redundant CRUD (có SaveChanges bên trong) |
| `Repository/UnitOfWork.cs` | Transaction + raw SQL operations |
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

*Tài liệu được tạo ngày 20/06/2026 dựa trên phân tích source code SmartCanteen.*
