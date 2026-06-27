# SmartCanteen - Class Diagram Mermaid for draw.io

Copy one Mermaid block at a time into draw.io: `Insert` -> `Advanced` -> `Mermaid`.

## 1. Main Domain Class Diagram

```mermaid
classDiagram
    direction LR

    class User {
        +Guid Id
        +string Name
        +string Email
        +Role Role
        +AccountStatus Status
        +bool EmailVerified
        +string StudentId
        +Money Balance
        +Register()
        +ConfirmEmail()
        +UpdateProfile()
        +UpdateBalance()
        +SoftDelete()
    }

    class Cart {
        +Guid Id
        +Guid UserId
        +string DataJson
        +long Version
        +Create()
        +Update()
    }

    class Category {
        +Guid Id
        +string Name
        +string Description
        +string ImgUrl
        +Create()
        +Update()
        +SoftDelete()
    }

    class Dish {
        +Guid Id
        +string Name
        +string Description
        +Money Price
        +bool IsActive
        +Guid CategoryId
        +Create()
        +Update()
        +MarkInactive()
        +SoftDelete()
    }

    class Session {
        +Guid Id
        +string Name
        +string Description
        +bool IsActive
        +DateTimeOffset AvailableFrom
        +DateTimeOffset AvailableTo
        +bool IsFinalized
        +AutoFinalizePolicy AutoFinalizePolicy
        +Finalize()
        +AddSessionDish()
        +AddMealTemplate()
        +SoftDelete()
    }

    class SessionDish {
        +Guid Id
        +Guid SessionId
        +Guid DishId
        +int Quantity
        +int PreparedQuantity
        +Create()
        +UpdateQuantity()
        +SetPreparedQuantity()
    }

    class MealTemplate {
        +Guid Id
        +Guid SessionId
        +string Name
        +Create()
        +AddSetting()
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
        +Create()
        +SoftDelete()
    }

    class Order {
        +Guid Id
        +Guid SessionId
        +Guid MealTemplateId
        +Guid WalletTransactionId
        +OrderStatus Status
        +Create()
        +AddDish()
        +AttachTransaction()
        +UpdateStatus()
        +SoftDelete()
    }

    class OrderItem {
        +int Id
        +Guid DishId
        +int Quantity
        +Money UnitPrice
        +OrderItemStatus ItemStatus
        +Create()
        +Confirm()
        +SwapDish()
        +RefundItem()
    }

    class OrderItemChangeProposal {
        +Guid Id
        +Guid OrderId
        +Guid UserId
        +Guid CurrentDishId
        +Guid SuggestedDishId
        +ChangeProposalStatus ProposalStatus
        +Create()
        +Accept()
        +RequestRefund()
    }

    class Money {
        +decimal Amount
        +string Currency
        +Create()
        +Add()
    }

    User "1" --> "0..1" Cart : has
    User "1" --> "0..*" Order : places
    User "1" --> "0..*" OrderItemChangeProposal : receives
    Category "1" --> "0..*" Dish : contains
    Dish *-- Money : price
    Session "1" *-- "0..*" SessionDish : offers
    Dish "1" --> "0..*" SessionDish : appears in
    Session "1" *-- "0..*" MealTemplate : defines
    MealTemplate "1" *-- "0..*" MealSettings : has rules
    Category "1" --> "0..*" MealSettings : used by
    Session "1" --> "0..*" Order : receives
    MealTemplate "0..1" --> "0..*" Order : used by
    Order "1" *-- "1..*" OrderItem : contains
    Dish "1" --> "0..*" OrderItem : ordered as
    OrderItem *-- Money : unit price
    Order "1" --> "0..*" OrderItemChangeProposal : has
    Dish "1" --> "0..*" OrderItemChangeProposal : current or suggested
```

## 2. Auth, Payment, Refund, Notification Class Diagram

```mermaid
classDiagram
    direction LR

    class User {
        +Guid Id
        +string Name
        +string Email
        +Money Balance
    }

    class EmailVerificationToken {
        +Guid Id
        +Guid UserId
        +string TokenHash
        +DateTimeOffset ExpiresAt
        +DateTimeOffset ConsumedAt
    }

    class PasswordResetToken {
        +Guid Id
        +Guid UserId
        +string TokenHash
        +DateTimeOffset ExpiresAt
        +DateTimeOffset ConsumedAt
    }

    class RefreshToken {
        +Guid Id
        +Guid UserId
        +string TokenHash
        +DateTimeOffset ExpiresAt
        +DateTimeOffset RevokedAt
    }

    class VerificationRequest {
        +Guid Id
        +Guid UserId
        +VerificationStatus Status
        +DateTimeOffset SubmittedAt
        +DateTimeOffset ExpiresAt
        +Submit()
        +Approve()
        +Reject()
        +MarkExpired()
    }

    class VerificationDocument {
        +Guid Id
        +Guid VerificationRequestId
        +DocumentType DocumentType
        +string CloudinaryUrl
        +string FileName
    }

    class Notification {
        +Guid Id
        +Guid RecipientId
        +string Type
        +string Title
        +string Message
        +bool IsRead
        +Create()
        +MarkAsRead()
        +SoftDelete()
    }

    class UserDeviceToken {
        +Guid Id
        +Guid UserId
        +string Token
        +string Platform
        +bool IsActive
        +Register()
        +Refresh()
        +Revoke()
    }

    class Payment {
        +Guid Id
        +Guid UserId
        +string GatewayOrderId
        +decimal AmountVnd
        +decimal ConvertedPoints
        +PaymentStatus Status
        +PaymentMethod Method
        +PaymentType Type
        +Create()
        +MarkAsCompleted()
        +MarkAsFailed()
    }

    class WalletTransaction {
        +Guid Id
        +Guid UserId
        +decimal Amount
        +decimal BalanceBefore
        +decimal BalanceAfter
        +WalletTransactionType TransactionType
        +Guid PaymentId
        +Create()
        +SoftDelete()
    }

    class Order {
        +Guid Id
        +Guid WalletTransactionId
        +OrderStatus Status
    }

    class RefundRequest {
        +Guid Id
        +Guid OrderId
        +Guid UserId
        +decimal RefundAmount
        +RefundRequestStatus Status
        +Guid WalletTransactionId
        +Submit()
        +Approve()
        +Reject()
    }

    class RefundRequestImage {
        +Guid Id
        +Guid RefundRequestId
        +string ImageUrl
        +string FileName
        +Create()
        +SoftDelete()
    }

    User "1" --> "0..*" EmailVerificationToken : has
    User "1" --> "0..*" PasswordResetToken : has
    User "1" --> "0..*" RefreshToken : has
    User "1" --> "0..*" VerificationRequest : submits
    VerificationRequest "1" *-- "1..*" VerificationDocument : documents
    User "1" --> "0..*" Notification : receives
    User "1" --> "0..*" UserDeviceToken : registers
    User "1" --> "0..*" Payment : makes
    User "1" --> "0..*" WalletTransaction : owns
    Payment "1" --> "0..1" WalletTransaction : creates
    WalletTransaction "0..1" --> "0..*" Order : pays
    Order "1" --> "0..1" RefundRequest : may have
    User "1" --> "0..*" RefundRequest : submits
    RefundRequest "1" --> "0..1" WalletTransaction : credited by
    RefundRequest "1" *-- "0..*" RefundRequestImage : evidence
```

## 3. Robot Serving Class Diagram

```mermaid
classDiagram
    direction LR

    class Order {
        +Guid Id
        +OrderStatus Status
    }

    class Session {
        +Guid Id
        +string Name
    }

    class Dish {
        +Guid Id
        +string Name
    }

    class RobotArm {
        +Guid Id
        +string Code
        +string IpAddress
        +int StationIndex
        +RobotArmStatus Status
        +Heartbeat()
        +UpdateStatus()
        +ChangeIp()
    }

    class ServingJob {
        +Guid Id
        +Guid OrderId
        +Guid TrayId
        +Guid RobotArmId
        +Guid PickupSlotId
        +ServingJobStatus Status
        +Create()
        +AssignTray()
        +MarkPushed()
        +Acknowledge()
        +MarkOnShelf()
        +MarkCollected()
        +MarkFailed()
    }

    class Tray {
        +Guid Id
        +string Code
        +TrayStatus Status
        +Guid CurrentOrderId
        +Create()
        +Reserve()
        +MarkInUse()
        +MarkAtSlot()
        +Release()
    }

    class PickupSlot {
        +Guid Id
        +string Code
        +PickupSlotStatus Status
        +Guid OrderId
        +Guid TrayId
        +Assign()
        +MarkWaitingCollect()
        +Clear()
    }

    class SlotConfiguration {
        +Guid Id
        +Guid SessionId
        +Guid DishId
        +Guid RobotArmId
        +string LaneCode
        +int Capacity
        +Create()
        +Update()
    }

    class ShelfStock {
        +Guid Id
        +Guid SessionId
        +Guid DishId
        +Guid SlotConfigurationId
        +int Quantity
        +Create()
        +TryDeduct()
        +Refill()
    }

    class RobotEventLog {
        +Guid Id
        +Guid RobotArmId
        +Guid ServingJobId
        +Guid OrderId
        +RobotEventType EventType
        +Create()
    }

    class AdminCommandAudit {
        +Guid Id
        +Guid RobotArmId
        +AdminCommandType CommandType
        +string Result
        +Create()
    }

    Order "1" --> "0..*" ServingJob : creates
    ServingJob "0..*" --> "0..1" RobotArm : executed by
    ServingJob "0..*" --> "0..1" Tray : uses
    ServingJob "0..*" --> "0..1" PickupSlot : delivered to
    Tray "0..*" --> "0..1" Order : current order
    PickupSlot "0..*" --> "0..1" Order : assigned order
    PickupSlot "0..*" --> "0..1" Tray : contains
    Session "1" --> "0..*" SlotConfiguration : configures
    Dish "1" --> "0..*" SlotConfiguration : assigned to
    RobotArm "1" --> "0..*" SlotConfiguration : controls
    Session "1" --> "0..*" ShelfStock : has stock
    Dish "1" --> "0..*" ShelfStock : stocked as
    SlotConfiguration "0..1" --> "0..*" ShelfStock : tracks
    RobotArm "1" --> "0..*" RobotEventLog : emits
    ServingJob "1" --> "0..*" RobotEventLog : logs
    Order "1" --> "0..*" RobotEventLog : logs
    RobotArm "1" --> "0..*" AdminCommandAudit : audited by
```
