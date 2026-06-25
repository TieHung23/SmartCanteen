# SmartCanteen - Class Diagram

> Class diagram duoc tong hop tu `SC.Domain/Domain`, `SC.Domain/Abstraction` va cac EF Core configuration trong `SC.Persistence/Database/Configuration`.
> Cac quan he trong diagram bao gom ca navigation property truc tiep va cac lien ket thuc te thong qua foreign key `Guid`.

## 1. Core Abstractions

```mermaid
classDiagram
    direction TB

    class IEntity_T {
        <<interface>>
        +T Id
    }

    class IAuditableEntity_T {
        <<interface>>
        +DateTimeOffset CreatedAtUtc
        +DateTimeOffset? UpdatedAtUtc
        +Guid CreatedBy
        +Guid UpdatedBy
    }

    class ISoftDeletable {
        <<interface>>
        +bool IsDeleted
        +DateTimeOffset? DeletedAtUtc
    }

    class Entity_T {
        <<abstract>>
        +T Id
    }

    class AggregateRoot_T {
        <<abstract>>
        -List~IDomainEvent~ _domainEvents
        +IReadOnlyCollection~IDomainEvent~ DomainEvents
        +ClearDomainEvents()
    }

    class ValueObject {
        <<abstract>>
    }

    class Money {
        +decimal Amount
        +string Currency
        +Create(decimal amount, string currency)$
        +Add(Money other)
    }

    Entity_T ..|> IEntity_T
    AggregateRoot_T --|> Entity_T
    Money --|> ValueObject
```

## 2. User, Authentication, Notification

```mermaid
classDiagram
    direction LR

    class User {
        +Guid Id
        +string Name
        +string Email
        +string? PasswordHash
        +string? GoogleSubjectId
        +string? ImgUrl
        +Role Role
        +AccountStatus Status
        +bool EmailVerified
        +string? StudentId
        +Money Balance
        +Register(...)$
        +RegisterWithGoogle(...)$
        +ConfirmEmail()
        +ActivateAfterIdentityApproved()
        +Suspend()
        +RecordLogin()
        +ChangePassword(string)
        +UpdateProfile(...)
        +UpdateBalance(Money)
        +SoftDelete()
    }

    class Cart {
        +Guid Id
        +Guid UserId
        +string DataJson
        +long Version
        +Create(Guid, string)$
        +Update(string, Guid)
    }

    class EmailVerificationToken {
        +Guid Id
        +Guid UserId
        +string TokenHash
        +DateTimeOffset ExpiresAt
        +DateTimeOffset? ConsumedAt
    }

    class PasswordResetToken {
        +Guid Id
        +Guid UserId
        +string TokenHash
        +DateTimeOffset ExpiresAt
        +DateTimeOffset? ConsumedAt
    }

    class RefreshToken {
        +Guid Id
        +Guid UserId
        +string TokenHash
        +DateTimeOffset ExpiresAt
        +DateTimeOffset? RevokedAt
        +Guid? ReplacedByTokenId
    }

    class VerificationRequest {
        +Guid Id
        +Guid UserId
        +VerificationStatus Status
        +DateTimeOffset SubmittedAt
        +DateTimeOffset ExpiresAt
        +IReadOnlyCollection~VerificationDocument~ Documents
        +Submit(...)$
        +Approve(Guid)
        +Reject(Guid, string)
        +MarkExpired()
    }

    class VerificationDocument {
        +Guid Id
        +Guid VerificationRequestId
        +DocumentType DocumentType
        +string CloudinaryUrl
        +string FileName
        +long FileSize
        +string MimeType
    }

    class Notification {
        +Guid Id
        +Guid RecipientId
        +string Type
        +string Title
        +string Message
        +Guid? ReferenceId
        +bool IsRead
        +Create(...)$
        +MarkAsRead(Guid)
        +SoftDelete()
    }

    class UserDeviceToken {
        +Guid Id
        +Guid UserId
        +string Token
        +string TokenHash
        +string Platform
        +bool IsActive
        +DateTimeOffset LastUsedAtUtc
        +Register(...)$
        +Refresh(...)
        +MarkUsed(Guid)
        +Revoke(Guid)
        +SoftDelete()
    }

    class Money {
        +decimal Amount
        +string Currency
    }

    class Role {
        <<enumeration>>
        Admin
        Manager
        User
        Staff
    }

    class AccountStatus {
        <<enumeration>>
        Active
        PendingEmailVerification
        PendingIdentityVerification
        Suspended
        Banned
    }

    class VerificationStatus {
        <<enumeration>>
        Pending
        Approved
        Rejected
        Expired
    }

    class DocumentType {
        <<enumeration>>
        StudentCard
        NationalId
        Other
    }

    User *-- Money : Balance
    User "1" --> "0..1" Cart : owns
    User "1" --> "0..*" EmailVerificationToken : has
    User "1" --> "0..*" PasswordResetToken : has
    User "1" --> "0..*" RefreshToken : has
    User "1" --> "0..*" VerificationRequest : submits
    VerificationRequest "1" *-- "1..*" VerificationDocument : documents
    User "1" --> "0..*" Notification : receives
    User "1" --> "0..*" UserDeviceToken : registers
    User ..> Role
    User ..> AccountStatus
    VerificationRequest ..> VerificationStatus
    VerificationDocument ..> DocumentType
```

## 3. Menu, Session, Order

```mermaid
classDiagram
    direction LR

    class Category {
        +Guid Id
        +string Name
        +string? Description
        +string? ImgUrl
        +Create(...)$
        +Update(...)
        +SoftDelete()
    }

    class Dish {
        +Guid Id
        +string Name
        +string Description
        +Money Price
        +bool IsActive
        +Guid CategoryId
        +string? ImgUrl
        +Create(...)$
        +Update(...)
        +MarkInactive(Guid)
        +SoftDelete()
    }

    class Session {
        +Guid Id
        +string Name
        +string Description
        +bool IsActive
        +DateTimeOffset AvailableFrom
        +DateTimeOffset AvailableTo
        +DateTimeOffset AvailableForOrder
        +DateTimeOffset? FinalizationDeadline
        +AutoFinalizePolicy AutoFinalizePolicy
        +bool IsFinalized
        +IReadOnlyCollection~SessionDish~ SessionDishes
        +IReadOnlyCollection~MealTemplate~ MealTemplates
        +Create(...)$
        +ConfigureFinalization(...)
        +Finalize(Guid)
        +AutoFinalize()
        +AddSessionDish(SessionDish)
        +AddMealTemplate(MealTemplate)
        +SoftDelete()
    }

    class SessionDish {
        +Guid Id
        +Guid DishId
        +Guid SessionId
        +int Quantity
        +int? PreparedQuantity
        +Create(...)$
        +UpdateQuantity(int)
        +SetPreparedQuantity(int)
    }

    class MealTemplate {
        +Guid Id
        +Guid SessionId
        +string Name
        +IReadOnlyCollection~MealSettings~ Settings
        +Create(...)$
        +AddSetting(Guid, int, int, bool)
        +ClearSettings()
        +SoftDelete()
    }

    class MealSettings {
        +Guid Id
        +Guid MealTemplateId
        +Guid CategoryId
        +int MinQuantity
        +int MaxQuantity
        +bool IsRequired
        +Create(...)$
        +SoftDelete()
    }

    class Order {
        +Guid Id
        +Guid SessionId
        +Guid? MealTemplateId
        +Guid? WalletTransactionId
        +OrderStatus Status
        +IReadOnlyCollection~OrderItem~ OrderItems
        +Create(...)$
        +ChangeSession(Guid, Guid)
        +AddDish(Guid, int, decimal)
        +AttachTransaction(Guid, Guid)
        +UpdateStatus(OrderStatus, Guid)
        +SoftDelete()
    }

    class OrderItem {
        +int Id
        +Guid DishId
        +int Quantity
        +Money UnitPrice
        +OrderItemStatus ItemStatus
        +Create(...)$
        +Confirm()
        +MarkChangePending()
        +SwapDish(Guid, decimal)
        +RefundItem()
    }

    class OrderItemChangeProposal {
        +Guid Id
        +Guid OrderId
        +Guid UserId
        +Guid CurrentDishId
        +Guid? SuggestedDishId
        +ChangeProposalStatus ProposalStatus
        +Create(...)$
        +Accept(Guid)
        +RequestRefund(Guid)
    }

    class OrderStatusHistory {
        +Guid Id
        +Guid OrderId
        +OrderStatus FromStatus
        +OrderStatus ToStatus
        +string? Note
        +Create(...)$
    }

    class Money {
        +decimal Amount
        +string Currency
    }

    class OrderStatus {
        <<enumeration>>
        Pending
        ReadyForPickup
        Completed
        Cancelled
        Preparing
        Serving
        InHoldingArea
        Expired
        Disposed
    }

    class OrderItemStatus {
        <<enumeration>>
        Pending
        Confirmed
        ChangePending
        Swapped
        Refunded
    }

    class ChangeProposalStatus {
        <<enumeration>>
        WaitingResponse
        Accepted
        RefundRequested
    }

    class AutoFinalizePolicy {
        <<enumeration>>
        AutoReject
        AutoConfirmAll
    }

    Category "1" --> "0..*" Dish : classifies
    Dish *-- Money : Price
    Session "1" *-- "0..*" SessionDish : offers
    SessionDish "*" --> "1" Dish : references
    Session "1" *-- "0..*" MealTemplate : templates
    MealTemplate "1" *-- "0..*" MealSettings : rules
    MealSettings "*" --> "1" Category : category rule
    Session "1" --> "0..*" Order : receives
    Order "1" *-- "1..*" OrderItem : items
    OrderItem "*" --> "1" Dish : ordered dish
    OrderItem *-- Money : UnitPrice
    Order "1" --> "0..*" OrderItemChangeProposal : change proposals
    OrderItemChangeProposal "*" --> "1" Dish : current dish
    OrderItemChangeProposal "*" --> "0..1" Dish : suggested dish
    Order "1" --> "0..*" OrderStatusHistory : status history
    Session ..> AutoFinalizePolicy
    Order ..> OrderStatus
    OrderItem ..> OrderItemStatus
    OrderItemChangeProposal ..> ChangeProposalStatus
```

## 4. Payment, Wallet, Refund

```mermaid
classDiagram
    direction LR

    class User {
        +Guid Id
        +Money Balance
    }

    class Payment {
        +Guid Id
        +Guid UserId
        +string GatewayOrderId
        +string? GatewayTransactionId
        +decimal AmountVnd
        +decimal ConvertedPoints
        +PaymentStatus Status
        +PaymentMethod Method
        +PaymentType Type
        +Create(...)$
        +MarkAsCompleted(string?, Guid)
        +MarkAsFailed(string?, Guid)
        +SoftDelete()
    }

    class WalletTransaction {
        +Guid Id
        +Guid UserId
        +decimal Amount
        +decimal BalanceBefore
        +decimal BalanceAfter
        +WalletTransactionType TransactionType
        +Guid? PaymentId
        +Create(...)$
        +SoftDelete()
    }

    class Order {
        +Guid Id
        +Guid? WalletTransactionId
        +OrderStatus Status
    }

    class RefundRequest {
        +Guid Id
        +Guid OrderId
        +Guid UserId
        +string PolicyCode
        +decimal RefundPercentSnapshot
        +decimal OrderAmountSnapshot
        +decimal RefundAmount
        +RefundRequestStatus Status
        +Guid? WalletTransactionId
        +IReadOnlyCollection~RefundRequestImage~ Images
        +Submit(...)$
        +AddImage(RefundRequestImage)
        +Approve(Guid, Guid)
        +Reject(Guid, string)
        +SoftDelete()
    }

    class RefundRequestImage {
        +Guid Id
        +Guid RefundRequestId
        +string ImageUrl
        +string FileName
        +DateTimeOffset UploadedAtUtc
        +Create(...)$
        +SoftDelete()
    }

    class PaymentStatus {
        <<enumeration>>
        Pending
        Completed
        Failed
    }

    class PaymentMethod {
        <<enumeration>>
        Momo
        ZaloPay
        VnPay
        SePay
        Wallet
    }

    class PaymentType {
        <<enumeration>>
        TopUp
        Subscription
        Refund
        OrderPayment
    }

    class WalletTransactionType {
        <<enumeration>>
        TopUp
        OrderPayment
        Refund
    }

    class RefundRequestStatus {
        <<enumeration>>
        Pending
        Approved
        Rejected
    }

    User "1" --> "0..*" Payment : makes
    User "1" --> "0..*" WalletTransaction : has ledger
    Payment "1" --> "0..1" WalletTransaction : creates top-up credit
    Order "0..1" --> "1" WalletTransaction : paid by
    Order "1" --> "0..1" RefundRequest : refund request
    RefundRequest "1" *-- "0..*" RefundRequestImage : evidence
    RefundRequest "0..1" --> "1" WalletTransaction : refund credit
    Payment ..> PaymentStatus
    Payment ..> PaymentMethod
    Payment ..> PaymentType
    WalletTransaction ..> WalletTransactionType
    RefundRequest ..> RefundRequestStatus
```

## 5. Robot Serving, Shelf, Pickup

```mermaid
classDiagram
    direction LR

    class RobotArm {
        +Guid Id
        +string Code
        +string? Name
        +string IpAddress
        +int StationIndex
        +RobotArmStatus Status
        +DateTimeOffset? LastHeartbeatUtc
        +Create(...)$
        +UpdateStatus(RobotArmStatus, Guid)
        +Heartbeat(Guid)
        +ChangeIp(string, Guid)
        +SoftDelete()
    }

    class ServingJob {
        +Guid Id
        +Guid OrderId
        +Guid? TrayId
        +Guid? RobotArmId
        +Guid? PickupSlotId
        +ServingJobStatus Status
        +DateTimeOffset? PushedAtUtc
        +DateTimeOffset? AcknowledgedAtUtc
        +DateTimeOffset? CompletedAtUtc
        +Create(...)$
        +AssignTray(Guid, Guid)
        +MarkPushed(Guid?, Guid)
        +Acknowledge(Guid)
        +MarkOnShelf(Guid, Guid)
        +MarkCollected(Guid)
        +MarkFailed(string, Guid)
        +Cancel(Guid)
        +SoftDelete()
    }

    class Tray {
        +Guid Id
        +string Code
        +TrayStatus Status
        +Guid? CurrentOrderId
        +Create(string, Guid)$
        +Reserve(Guid, Guid)
        +MarkInUse(Guid)
        +MarkAtSlot(Guid)
        +Release(Guid)
        +SoftDelete()
    }

    class PickupSlot {
        +Guid Id
        +string Code
        +PickupSlotStatus Status
        +Guid? OrderId
        +Guid? TrayId
        +bool? SensorOccupied
        +Create(...)$
        +Assign(Guid, Guid, Guid)
        +MarkWaitingCollect(Guid)
        +Clear(Guid)
        +SoftDelete()
    }

    class SlotConfiguration {
        +Guid Id
        +Guid SessionId
        +Guid DishId
        +Guid? RobotArmId
        +string LaneCode
        +int Capacity
        +Create(...)$
        +Update(...)
        +SoftDelete()
    }

    class ShelfStock {
        +Guid Id
        +Guid SessionId
        +Guid DishId
        +Guid? SlotConfigurationId
        +int Quantity
        +Create(...)$
        +TryDeduct(int, Guid)
        +Refill(int, Guid)
        +SoftDelete()
    }

    class RobotEventLog {
        +Guid Id
        +Guid? RobotArmId
        +Guid? ServingJobId
        +Guid? OrderId
        +RobotEventType EventType
        +string? Message
        +string? PayloadJson
        +DateTimeOffset OccurredAtUtc
        +Create(...)$
        +SoftDelete()
    }

    class AdminCommandAudit {
        +Guid Id
        +AdminCommandType CommandType
        +Guid? RobotArmId
        +string? ParametersJson
        +string? Result
        +Create(...)$
        +SoftDelete()
    }

    class Order {
        +Guid Id
        +OrderStatus Status
    }

    class Session {
        +Guid Id
    }

    class Dish {
        +Guid Id
    }

    class RobotArmStatus {
        <<enumeration>>
        Offline
        Idle
        Busy
        Error
        Maintenance
    }

    class ServingJobStatus {
        <<enumeration>>
        Queued
        Pushed
        Assembling
        OnShelf
        Collected
        Failed
        Cancelled
    }

    class TrayStatus {
        <<enumeration>>
        Available
        Reserved
        InUse
        AtSlot
    }

    class PickupSlotStatus {
        <<enumeration>>
        Empty
        Occupied
        WaitingCollect
    }

    class RobotEventType {
        <<enumeration>>
        Connected
        Disconnected
        JobReceived
        PickStarted
        PickCompleted
        PlaceCompleted
        Error
        Recovered
        EmergencyStop
    }

    class AdminCommandType {
        <<enumeration>>
        StartRobot
        StopRobot
        ResetError
        ChangeIp
        ReassignSlot
        Other
    }

    Order "1" --> "0..*" ServingJob : serving jobs
    ServingJob "*" --> "0..1" Tray : uses
    ServingJob "*" --> "0..1" RobotArm : executed by
    ServingJob "*" --> "0..1" PickupSlot : delivered to
    Tray "*" --> "0..1" Order : current order
    PickupSlot "*" --> "0..1" Order : assigned order
    PickupSlot "*" --> "0..1" Tray : contains tray
    Session "1" --> "0..*" SlotConfiguration : slot plan
    Dish "1" --> "0..*" SlotConfiguration : stored in lane
    RobotArm "1" --> "0..*" SlotConfiguration : controls lane
    Session "1" --> "0..*" ShelfStock : shelf stock
    Dish "1" --> "0..*" ShelfStock : stocked dish
    SlotConfiguration "1" --> "0..*" ShelfStock : tracks stock
    RobotArm "1" --> "0..*" RobotEventLog : emits
    ServingJob "1" --> "0..*" RobotEventLog : logs
    Order "1" --> "0..*" RobotEventLog : logs
    RobotArm "1" --> "0..*" AdminCommandAudit : command target
    RobotArm ..> RobotArmStatus
    ServingJob ..> ServingJobStatus
    Tray ..> TrayStatus
    PickupSlot ..> PickupSlotStatus
    RobotEventLog ..> RobotEventType
    AdminCommandAudit ..> AdminCommandType
```

## 6. Setting and Logging

```mermaid
classDiagram
    direction LR

    class Setting {
        +Guid Id
        +string Code
        +string Name
        +string Description
        +string Group
        +string Scope
        +string Value
        +string Type
        +Create(...)$
        +Update(...)
        +SoftDelete()
    }

    class ApiLog {
        +Guid Id
        +string? LoginId
        +string LogLevel
        +string ApiUrl
        +string ApiMethod
        +string? Message
        +string? ErrorTrace
        +string? ApiBody
        +string? ApiResponse
        +int StatusCode
        +DateTimeOffset CreatedDate
        +DateTimeOffset? EndDate
    }
```

## 7. Cross-Module Dependency Summary

```mermaid
classDiagram
    direction TB

    class User
    class Category
    class Dish
    class Session
    class MealTemplate
    class Order
    class Payment
    class WalletTransaction
    class RefundRequest
    class VerificationRequest
    class Notification
    class RobotArm
    class ServingJob
    class Tray
    class PickupSlot
    class SlotConfiguration
    class ShelfStock

    User --> Payment : top-up/payment owner
    User --> WalletTransaction : wallet ledger
    User --> Order : CreatedBy places order
    User --> RefundRequest : submits
    User --> VerificationRequest : verifies identity
    User --> Notification : receives

    Category --> Dish : categorizes
    Category --> MealTemplate : via MealSettings
    Dish --> Session : via SessionDish
    Session --> MealTemplate : defines
    Session --> Order : order window
    Order --> Dish : via OrderItem
    Order --> WalletTransaction : paid/refunded through
    Payment --> WalletTransaction : top-up creates
    Order --> RefundRequest : optional refund

    Order --> ServingJob : robot serving workflow
    ServingJob --> RobotArm : executed by
    ServingJob --> Tray : uses
    ServingJob --> PickupSlot : delivered to
    Session --> SlotConfiguration : station setup
    SlotConfiguration --> ShelfStock : stock by slot
    Dish --> ShelfStock : stock item
```

## Notes

- `AggregateRoot<Guid>` va `Entity<T>` la lop nen cua hau het domain class.
- `Money` la value object duoc embed trong `User.Balance`, `Dish.Price`, `OrderItem.UnitPrice`.
- Mot so quan he trong code duoc bieu dien bang foreign key `Guid` thay vi navigation property, vi vay diagram dung mui ten association/dependency de phan anh quan he thuc te.
- `OrderItem` va `VerificationDocument` duoc EF Core cau hinh nhu owned/contained item cua aggregate cha.
- Cac class deu co audit fields (`CreatedAtUtc`, `UpdatedAtUtc`, `CreatedBy`, `UpdatedBy`) neu implement `IAuditableEntity<Guid>`; diagram chi hien thi trong nhung class quan trong de tranh qua tai.
