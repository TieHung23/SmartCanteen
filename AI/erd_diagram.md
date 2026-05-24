# SmartCanteen — ERD Diagram

> Entity-Relationship Diagram with full column-level detail, derived from `SC.Domain/Domain`.

```mermaid
erDiagram
    USER {
        Guid Id PK
        string Name
        string Email
        string PasswordHash
        string ImgUrl "nullable"
        int Role "Enum: Admin(1) Manager(2) User(3)"
        int Category "Enum: Student(1) Lecturer(2) Staff(3) External(4)"
        int Status "Enum: Active(1) PendingEmailVerification(2) PendingIdentityVerification(3) Suspended(4) Banned(5)"
        bool EmailVerified
        string StudentId "nullable"
        DateOnly DateOfBirth "nullable"
        string MajorOrClass "nullable"
        string PhoneNumber "nullable"
        string Address "nullable"
        int Gender "Enum: Male(1) Female(2) Other(3)"
        DateTimeOffset LastLoginAt "nullable"
        decimal BalanceAmount "Value Object: Money"
        string BalanceCurrency "Value Object: Money"
        DateTimeOffset CreatedAtUtc
        DateTimeOffset UpdatedAtUtc "nullable"
        Guid CreatedBy
        Guid UpdatedBy
    }

    SETTING {
        Guid Id PK
        string Code
        string Name
        string Description
        string Group
        string Value
        string Type
        DateTimeOffset CreatedAtUtc
        DateTimeOffset UpdatedAtUtc "nullable"
        Guid CreatedBy
        Guid UpdatedBy
    }

    API_LOG {
        Guid Id PK
        string LoginId "nullable"
        string LogLevel
        string ApiUrl
        string ApiMethod
        string Message "nullable"
        string ErrorTrace "nullable"
        string ApiBody "nullable"
        string ApiResponse "nullable"
        string LocalIpAddress "nullable"
        string LocalHostPC "nullable"
        string LogApp "nullable"
        string LogVersion "nullable"
        string Memo "nullable"
        string RequestId "nullable"
        string ApiDesc "nullable"
        string ApiVer "nullable"
        DateTimeOffset CreatedDate
        DateTimeOffset EndDate "nullable"
    }

    CATEGORY {
        Guid Id PK
        string Name
        string Description
        DateTimeOffset CreatedAtUtc
        DateTimeOffset UpdatedAtUtc "nullable"
        Guid CreatedBy
        Guid UpdatedBy
    }

    MEAL {
        Guid Id PK
        string Name
        string Description
        decimal PriceAmount "Value Object: Money"
        string PriceCurrency "Value Object: Money"
        bool IsActive
        DateTimeOffset AvailableFrom
        DateTimeOffset AvailableTo
        DateTimeOffset AvailableForOrder
        DateTimeOffset CreatedAtUtc
        DateTimeOffset UpdatedAtUtc "nullable"
        Guid CreatedBy
        Guid UpdatedBy
    }

    MEAL_SETTINGS {
        Guid MealId FK
        Guid CategoryId FK
        int Quantity
        bool IsDeleted
    }

    DISH {
        Guid Id PK
        string Name
        string Description
        decimal PriceAmount "Value Object: Money"
        string PriceCurrency "Value Object: Money"
        int StockQuantity
        bool IsActive
        Guid MealId FK
        Guid CategoryId FK
        DateTimeOffset CreatedAtUtc
        DateTimeOffset UpdatedAtUtc "nullable"
        Guid CreatedBy
        Guid UpdatedBy
    }

    ORDER {
        Guid Id PK
        Guid MealId FK
        Guid PaymentId FK "nullable"
        int Status "Enum: Pending(0) ReadyForPickup(1) Completed(2) Cancelled(3)"
        DateTimeOffset CreatedAtUtc
        DateTimeOffset UpdatedAtUtc "nullable"
        Guid CreatedBy FK "references User"
        Guid UpdatedBy
    }

    ORDER_ITEM {
        Guid OrderId FK
        Guid DishId FK
        int Quantity
        decimal UnitPriceAmount "Value Object: Money"
        string UnitPriceCurrency "Value Object: Money"
    }

    PAYMENT {
        Guid Id PK
        decimal BalanceSnapshotDeltaAmount "Value Object: BalanceSnapshot"
        decimal BalanceSnapshotBalanceBefore "Value Object: BalanceSnapshot"
        decimal BalanceSnapshotBalanceAfter "Value Object: BalanceSnapshot"
        string GatewayTransactionId
        int Status "Enum: Pending(1) Completed(2) Failed(3)"
        int Method "Enum: Momo(1) ZaloPay(2) VnPay(3)"
        int Type "Enum: TopUp(1) Subscription(2) Refund(3)"
        Guid UserId FK
        DateTimeOffset CreatedAtUtc
        DateTimeOffset UpdatedAtUtc "nullable"
        Guid CreatedBy
        Guid UpdatedBy
    }

    VERIFICATION_REQUEST {
        Guid Id PK
        Guid UserId FK
        int Status "Enum: Pending(1) Approved(2) Rejected(3) Expired(4)"
        DateTimeOffset SubmittedAt
        DateTimeOffset ReviewedAt "nullable"
        Guid ReviewedBy "nullable"
        string RejectionReason "nullable"
        DateTimeOffset ExpiresAt
        DateTimeOffset CreatedAtUtc
        DateTimeOffset UpdatedAtUtc "nullable"
        Guid CreatedBy
        Guid UpdatedBy
    }

    VERIFICATION_DOCUMENT {
        Guid Id PK
        Guid VerificationRequestId FK
        int DocumentType "Enum: StudentCard(1) NationalId(2) Other(3)"
        string CloudinaryUrl
        string FileName
        long FileSize
        string MimeType
        DateTimeOffset UploadedAt
    }

    %% ── Relationships ──

    USER ||--o{ PAYMENT : "makes"
    USER ||--o{ ORDER : "places (CreatedBy)"
    USER ||--o{ VERIFICATION_REQUEST : "submits"

    MEAL ||--o{ MEAL_SETTINGS : "has settings"
    CATEGORY ||--o{ MEAL_SETTINGS : "used in"

    MEAL ||--o{ DISH : "contains"
    CATEGORY ||--o{ DISH : "classifies"

    MEAL ||--o{ ORDER : "ordered as"
    ORDER ||--o{ ORDER_ITEM : "contains"
    DISH ||--o{ ORDER_ITEM : "referenced by"

    ORDER |o--o| PAYMENT : "paid via"

    VERIFICATION_REQUEST ||--o{ VERIFICATION_DOCUMENT : "contains"
```

## Relationship Summary

| Parent                  | Child                    | Cardinality | FK Column               | Description                                        |
| ----------------------- | ------------------------ | ----------- | ----------------------- | -------------------------------------------------- |
| **Meal**                | **MealSettings**         | 1 → \*      | `MealId`                | A meal defines category-quantity rules             |
| **Category**            | **MealSettings**         | 1 → \*      | `CategoryId`            | A category can appear in many meal settings        |
| **Meal**                | **Dish**                 | 1 → \*      | `MealId`                | A meal has many dishes                             |
| **Category**            | **Dish**                 | 1 → \*      | `CategoryId`            | A category classifies many dishes                  |
| **User**                | **Payment**              | 1 → \*      | `UserId`                | A user makes many payments                         |
| **User**                | **Order**                | 1 → \*      | `CreatedBy`             | A user places many orders                          |
| **Meal**                | **Order**                | 1 → \*      | `MealId`                | Orders are placed for a specific meal              |
| **Order**               | **OrderItem**            | 1 → \*      | `OrderId`               | An order contains multiple line items              |
| **Dish**                | **OrderItem**            | 1 → \*      | `DishId`                | A dish can appear in many order items              |
| **Payment**             | **Order**                | 0..1 → 0..1 | `PaymentId`             | An order may optionally be linked to a payment     |
| **User**                | **VerificationRequest**  | 1 → \*      | `UserId`                | A user submits verification requests               |
| **VerificationRequest** | **VerificationDocument** | 1 → \*      | `VerificationRequestId` | A verification request contains multiple documents |

## Owned Value Objects (flattened into parent table)

| Value Object        | Owner                 | Columns                                                                                     |
| ------------------- | --------------------- | ------------------------------------------------------------------------------------------- |
| **Money**           | User (Balance)        | `BalanceAmount`, `BalanceCurrency`                                                          |
| **Money**           | Meal (Price)          | `PriceAmount`, `PriceCurrency`                                                              |
| **Money**           | Dish (Price)          | `PriceAmount`, `PriceCurrency`                                                              |
| **Money**           | OrderItem (UnitPrice) | `UnitPriceAmount`, `UnitPriceCurrency`                                                      |
| **BalanceSnapshot** | Payment               | `BalanceSnapshotDeltaAmount`, `BalanceSnapshotBalanceBefore`, `BalanceSnapshotBalanceAfter` |

## Enums (stored as integer columns)

| Enum                   | Values                                                                                               | Used In                           |
| ---------------------- | ---------------------------------------------------------------------------------------------------- | --------------------------------- |
| **Role**               | Admin (1), Manager (2), User (3)                                                                     | User.Role                         |
| **UserCategory**       | Student (1), Lecturer (2), Staff (3), External (4)                                                   | User.Category                     |
| **AccountStatus**      | Active (1), PendingEmailVerification (2), PendingIdentityVerification (3), Suspended (4), Banned (5) | User.Status                       |
| **Gender**             | Male (1), Female (2), Other (3)                                                                      | User.Gender                       |
| **OrderStatus**        | Pending (0), ReadyForPickup (1), Completed (2), Cancelled (3)                                        | Order.Status                      |
| **PaymentStatus**      | Pending (1), Completed (2), Failed (3)                                                               | Payment.Status                    |
| **PaymentMethod**      | Momo (1), ZaloPay (2), VnPay (3)                                                                     | Payment.Method                    |
| **PaymentType**        | TopUp (1), Subscription (2), Refund (3)                                                              | Payment.Type                      |
| **DocumentType**       | StudentCard (1), NationalId (2), Other (3)                                                           | VerificationDocument.DocumentType |
| **VerificationStatus** | Pending (1), Approved (2), Rejected (3), Expired (4)                                                 | VerificationRequest.Status        |
