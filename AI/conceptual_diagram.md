# SmartCanteen — Conceptual Diagram

> Core domain aggregates and their relationships with cardinality.

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

    Meal -->|"1 → n"| MealTemplate
    Meal -->|"n → n"| Dish
    Dish -->|"n → 1"| Category
    MealTemplate -->|"n → 1"| Category

    Cart -->|"1 → 1"| User

    Order -->|"n → 1"| Meal
    Order -->|"n → n"| Dish
    Order -->|"n → 0..1"| WalletTransaction
    Order -->|"n → 0..1"| MealTemplate

    Payment -->|"1 → 0..1"| WalletTransaction
    WalletTransaction -->|"n → 1"| User
    Payment -->|"n → 1"| User

    RefundRequest -->|"1 → 0..1"| Order
    RefundRequest -->|"n → 1"| User
    RefundRequest -->|"1 → 0..1"| WalletTransaction

    User -->|"1 → n"| Order
    User -->|"1 → n"| VerificationRequest
    User -->|"1 → n"| PasswordResetToken
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
