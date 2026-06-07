# SmartCanteen — Conceptual Diagram

> Core domain aggregates and their relationships.

```mermaid
graph LR
    User["👤 User"]
    Meal["🍽️ Meal"]
    Dish["🥘 Dish"]
    Category["🏷️ Category"]
    MealTemplate["📋 MealTemplate"]
    Order["📦 Order"]
    Payment["💳 Payment"]
    WalletTransaction["💰 WalletTransaction"]
    VerificationRequest["✅ VerificationRequest"]
    Setting["⚙️ Setting"]

    Meal -->|has| MealTemplate
    Meal -->|contains| Dish
    Dish -->|classified by| Category
    MealTemplate -->|defines rules for| Category

    Order -->|for| Meal
    Order -->|contains| Dish
    Order -->|paid via| WalletTransaction

    Payment -->|credits| WalletTransaction
    WalletTransaction -->|belongs to| User
    Payment -->|made by| User

    User -->|places| Order
    User -->|submits| VerificationRequest
    User -->|configures| Setting
```

## Domain Summary

| Concept              | DDD Stereotype | Key Responsibility                                                    |
| -------------------- | -------------- | --------------------------------------------------------------------- |
| **User**             | Aggregate Root | Identity, auth, wallet balance, profile, verification                 |
| **Meal**             | Aggregate Root | Time-bound meal plan with dish composition & customisation templates   |
| **MealTemplate**     | Entity         | Named template grouping category-quantity rules within a meal          |
| **Dish**             | Aggregate Root | Menu item with price, category, image                                  |
| **Category**         | Aggregate Root | Classification for dishes and meal settings                           |
| **Order**            | Aggregate Root | Customer request linked to meal, line items, wallet transaction        |
| **Payment**          | Aggregate Root | Third-party gateway transaction tracking                               |
| **WalletTransaction**| Entity         | Immutable record of every wallet balance change (top-up, order, refund)|
| **VerificationRequest** | Aggregate Root | Identity verification with document uploads and admin review         |
| **Setting**          | Aggregate Root | App-wide configuration key-value pairs                                 |
