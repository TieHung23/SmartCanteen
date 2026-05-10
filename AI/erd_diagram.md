# SmartCanteen — ERD Diagram

> Entity-Relationship Diagram with full column-level detail, derived from `SC.Domain/Domain`.

```mermaid
erDiagram
    USER {
        Guid Id PK
        string Name
        string Email
        string PasswordHash
        string ImgUrl
        int Role "Enum: Admin(1) Manager(2) User(3)"
        decimal BalanceAmount "Value Object: Money"
        string BalanceCurrency "Value Object: Money"
        DateTimeOffset CreatedAtUtc
        DateTimeOffset UpdatedAtUtc "nullable"
        Guid CreatedBy
        Guid UpdatedBy
    }

    SETTING {
        Guid Id PK
        string Key
        string Value
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

    %% ── Relationships ──

    USER ||--o{ PAYMENT : "makes"
    USER ||--o{ ORDER : "places (CreatedBy)"

    MEAL ||--o{ MEAL_SETTINGS : "has settings"
    CATEGORY ||--o{ MEAL_SETTINGS : "used in"

    MEAL ||--o{ DISH : "contains"
    CATEGORY ||--o{ DISH : "classifies"

    MEAL ||--o{ ORDER : "ordered as"
    ORDER ||--o{ ORDER_ITEM : "contains"
    DISH ||--o{ ORDER_ITEM : "referenced by"

    ORDER |o--o| PAYMENT : "paid via"
```

## Relationship Summary

| Parent | Child | Cardinality | FK Column | Description |
|--------|-------|-------------|-----------|-------------|
| **Meal** | **MealSettings** | 1 → * | `MealId` | A meal defines category-quantity rules |
| **Category** | **MealSettings** | 1 → * | `CategoryId` | A category can appear in many meal settings |
| **Meal** | **Dish** | 1 → * | `MealId` | A meal has many dishes |
| **Category** | **Dish** | 1 → * | `CategoryId` | A category classifies many dishes |
| **User** | **Payment** | 1 → * | `UserId` | A user makes many payments |
| **User** | **Order** | 1 → * | `CreatedBy` | A user places many orders |
| **Meal** | **Order** | 1 → * | `MealId` | Orders are placed for a specific meal |
| **Order** | **OrderItem** | 1 → * | `OrderId` | An order contains multiple line items |
| **Dish** | **OrderItem** | 1 → * | `DishId` | A dish can appear in many order items |
| **Payment** | **Order** | 0..1 → 0..1 | `PaymentId` | An order may optionally be linked to a payment |

## Owned Value Objects (flattened into parent table)

| Value Object | Owner | Columns |
|-------------|-------|---------|
| **Money** | User (Balance) | `BalanceAmount`, `BalanceCurrency` |
| **Money** | Meal (Price) | `PriceAmount`, `PriceCurrency` |
| **Money** | Dish (Price) | `PriceAmount`, `PriceCurrency` |
| **Money** | OrderItem (UnitPrice) | `UnitPriceAmount`, `UnitPriceCurrency` |
| **BalanceSnapshot** | Payment | `BalanceSnapshotDeltaAmount`, `BalanceSnapshotBalanceBefore`, `BalanceSnapshotBalanceAfter` |

## Enums (stored as integer columns)

| Enum | Values | Used In |
|------|--------|---------|
| **Role** | Admin (1), Manager (2), User (3) | User.Role |
| **OrderStatus** | Pending (0), ReadyForPickup (1), Completed (2), Cancelled (3) | Order.Status |
| **PaymentStatus** | Pending (1), Completed (2), Failed (3) | Payment.Status |
| **PaymentMethod** | Momo (1), ZaloPay (2), VnPay (3) | Payment.Method |
| **PaymentType** | TopUp (1), Subscription (2), Refund (3) | Payment.Type |
