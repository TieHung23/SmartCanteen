# SmartCanteen — Conceptual Diagram

> Core domain aggregates and their relationships.

```mermaid
graph LR
    User["👤 User"]
    Meal["🍽️ Meal"]
    Dish["🥘 Dish"]
    Category["🏷️ Category"]
    MealTemplate["📋 MealTemplate"]
    Cart["🛒 Cart"]
    Order["📦 Order"]
    Payment["💳 Payment"]
    WalletTransaction["💰 WalletTransaction"]
    RefundRequest["🔄 RefundRequest"]
    VerificationRequest["✅ VerificationRequest"]
    Setting["⚙️ Setting"]
    PasswordResetToken["🔑 PasswordResetToken"]

    Meal -->|has| MealTemplate
    Meal -->|contains| Dish
    Dish -->|classified by| Category
    MealTemplate -->|defines rules for| Category

    Cart -->|belongs to| User

    Order -->|for| Meal
    Order -->|contains| Dish
    Order -->|paid via| WalletTransaction
    Order -->|optionally uses| MealTemplate

    Payment -->|credits| WalletTransaction
    WalletTransaction -->|belongs to| User
    Payment -->|made by| User

    RefundRequest -->|for| Order
    RefundRequest -->|submitted by| User
    RefundRequest -->|credited via| WalletTransaction

    User -->|places| Order
    User -->|submits| VerificationRequest
    User -->|configures| Setting
    User -->|has| PasswordResetToken
```

## Domain Summary

| Concept               | DDD Stereotype | Key Responsibility                                                    |
| --------------------- | -------------- | --------------------------------------------------------------------- |
| **User**              | Aggregate Root | Identity, auth, wallet balance, profile, verification                 |
| **Meal**              | Aggregate Root | Time-bound meal plan with dish composition & customisation templates   |
| **MealTemplate**      | Entity         | Named template grouping category-quantity rules within a meal          |
| **Dish**              | Aggregate Root | Menu item with price, category, image                                  |
| **Category**          | Aggregate Root | Classification for dishes and meal settings                           |
| **Cart**              | Aggregate Root | Per-user shopping cart stored as JSON with optimistic concurrency      |
| **Order**             | Aggregate Root | Customer request linked to meal, line items, wallet transaction        |
| **Payment**           | Aggregate Root | Third-party gateway transaction tracking (top-up payments)             |
| **WalletTransaction** | Entity         | Immutable record of every wallet balance change (top-up, order, refund)|
| **RefundRequest**     | Aggregate Root | Customer refund claim with policy snapshots, images, and admin review  |
| **VerificationRequest** | Aggregate Root | Identity verification with document uploads and admin review         |
| **Setting**           | Aggregate Root | App-wide configuration key-value pairs (including refund policies)     |
| **PasswordResetToken** | Aggregate Root | Single-use time-limited token for password reset flow                 |
