# SmartCanteen — Conceptual Diagram

> High-level business domain relationships derived from `SC.Domain/Domain`.

```mermaid
graph TB
    subgraph Legend
        direction LR
        L1["🟦 Aggregate Root"]
        L2["🟩 Entity"]
        L3["🟨 Value Object"]
        L4["🟪 Enum"]
    end

    subgraph Shared Kernel
        MONEY["🟨 Money<br/><i>Value Object</i>"]
    end

    subgraph User Bounded Context
        USER["🟦 User<br/><i>Aggregate Root</i>"]
        ROLE["🟪 Role<br/><i>Enum</i>"]
    end

    subgraph Meal Bounded Context
        MEAL["🟦 Meal<br/><i>Aggregate Root</i>"]
        MSETTINGS["🟨 MealSettings<br/><i>Value Object</i>"]
    end

    subgraph Category Bounded Context
        CATEGORY["🟦 Category<br/><i>Aggregate Root</i>"]
    end

    subgraph Dish Bounded Context
        DISH["🟦 Dish<br/><i>Aggregate Root</i>"]
    end

    subgraph Order Bounded Context
        ORDER["🟦 Order<br/><i>Aggregate Root</i>"]
        OITEM["🟨 OrderItem<br/><i>Value Object</i>"]
    end

    subgraph Payment Bounded Context
        PAYMENT["🟦 Payment<br/><i>Aggregate Root</i>"]
        BSNAPSHOT["🟨 BalanceSnapshot<br/><i>Value Object</i>"]
        PSTATUS["🟪 PaymentStatus<br/><i>Enum</i>"]
        PMETHOD["🟪 PaymentMethod<br/><i>Enum</i>"]
        PTYPE["🟪 PaymentType<br/><i>Enum</i>"]
    end

    %% ── Relationships ──

    USER -- "uses" --> MONEY
    USER -- "assigned" --> ROLE

    MEAL -- "contains 1..*" --> MSETTINGS
    MSETTINGS -. "references by ID" .-> CATEGORY
    MSETTINGS -- "belongs to" --> MEAL
    MEAL -- "priced with" --> MONEY

    DISH -. "belongs to by ID" .-> MEAL
    DISH -. "categorised by ID" .-> CATEGORY
    DISH -- "priced with" --> MONEY

    ORDER -. "for by ID" .-> MEAL
    ORDER -- "contains 1..*" --> OITEM
    ORDER -. "paid via by ID 0..1" .-> PAYMENT
    OITEM -. "references by ID" .-> DISH
    OITEM -- "priced with" --> MONEY

    PAYMENT -. "made by ID" .-> USER
    PAYMENT -- "tracks" --> BSNAPSHOT
    PAYMENT -- "status" --> PSTATUS
    PAYMENT -- "method" --> PMETHOD
    PAYMENT -- "type" --> PTYPE

    USER -. "creates" .-> ORDER
    USER -. "creates" .-> MEAL
    USER -. "creates" .-> DISH
    USER -. "creates" .-> CATEGORY

    style USER fill:#4A90D9,color:#fff
    style MEAL fill:#4A90D9,color:#fff
    style DISH fill:#4A90D9,color:#fff
    style ORDER fill:#4A90D9,color:#fff
    style PAYMENT fill:#4A90D9,color:#fff
    style CATEGORY fill:#4A90D9,color:#fff
    style MONEY fill:#FFD700,color:#333
    style MSETTINGS fill:#FFD700,color:#333
    style OITEM fill:#FFD700,color:#333
    style BSNAPSHOT fill:#FFD700,color:#333
    style ROLE fill:#9B59B6,color:#fff
    style PSTATUS fill:#9B59B6,color:#fff
    style PMETHOD fill:#9B59B6,color:#fff
    style PTYPE fill:#9B59B6,color:#fff
```

## Domain Summary

| Concept | DDD Stereotype | Key Responsibility |
|---------|---------------|-------------------|
| **User** | Aggregate Root | Manages identity, authentication, balance |
| **Meal** | Aggregate Root | Defines meal plans with category-based settings |
| **Dish** | Aggregate Root | Menu items with price, stock, and categorisation |
| **Order** | Aggregate Root | Customer order linked to a meal and its line items |
| **Payment** | Aggregate Root | Tracks financial transactions and balance changes |
| **Category** | Aggregate Root | Classification for dishes and meal settings |
| **Money** | Value Object | (Shared Kernel) Immutable monetary amount with currency |
| **MealSettings** | Value Object | Category-quantity rules within a meal |
| **OrderItem** | Value Object | Line item: dish + quantity + unit price |
| **BalanceSnapshot** | Value Object | Before/after balance for a payment |
| **Role** | Enum | Admin · Manager · User |
| **PaymentStatus** | Enum | Pending · Completed · Failed |
| **PaymentMethod** | Enum | Momo · ZaloPay · VnPay |
| **PaymentType** | Enum | TopUp · Subscription · Refund |
