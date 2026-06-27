# SmartCanteen - Conceptual Model Mermaid

> Mermaid code based on `AI/conceptual_diagram.md`.
> Paste this into draw.io: `Insert` -> `Advanced` -> `Mermaid`.

```mermaid
flowchart LR
    %% SmartCanteen Conceptual Model
    %% Cardinality:
    %% 1 to 1    = one mandatory to one mandatory
    %% 1 to 0..1 = one mandatory to one optional
    %% 1 to *    = one mandatory to many
    %% * to 1    = many to one mandatory
    %% * to *    = many to many
    %% * to 0..1 = many to one optional

    User["User"]
    Session["Session"]
    Dish["Dish"]
    Category["Category"]
    MealTemplate["MealTemplate"]
    Cart["Cart"]
    Order["Order"]
    Payment["Payment"]
    WalletTransaction["WalletTransaction"]
    RefundRequest["RefundRequest"]
    VerificationRequest["VerificationRequest"]
    Setting["Setting"]
    Notification["Notification"]
    ChangeProposal["OrderItemChangeProposal"]
    PasswordResetToken["PasswordResetToken"]

    Session -->|"1 to *"| MealTemplate
    Session -->|"* to * via SessionDish"| Dish
    Dish -->|"* to 1"| Category
    MealTemplate -->|"* to 1 via MealSettings"| Category

    User -->|"1 to 1"| Cart

    Order -->|"* to 1"| Session
    Order -->|"* to * via OrderItem"| Dish
    Order -->|"* to 0..1"| WalletTransaction
    Order -->|"* to 0..1"| MealTemplate
    Order -->|"1 to *"| ChangeProposal

    Payment -->|"1 to 0..1"| WalletTransaction
    WalletTransaction -->|"* to 1"| User
    Payment -->|"* to 1"| User

    RefundRequest -->|"0..1 to 0..1"| Order
    RefundRequest -->|"* to 1"| User
    RefundRequest -->|"0..1 to 1"| WalletTransaction

    ChangeProposal -->|"* to 1"| Order
    ChangeProposal -->|"* to 1"| User

    User -->|"1 to *"| Order
    User -->|"1 to *"| Notification
    User -->|"1 to *"| VerificationRequest
    User -->|"1 to *"| PasswordResetToken
    User -->|"1 to *"| ChangeProposal

    Setting -.->|"configures"| Session
    Setting -.->|"configures refund policies"| RefundRequest

    classDef user fill:#E8F1FF,stroke:#2F80ED,color:#111827
    classDef menu fill:#EAF7EA,stroke:#27AE60,color:#111827
    classDef order fill:#FFF4E5,stroke:#F2994A,color:#111827
    classDef payment fill:#F5EAFE,stroke:#9B51E0,color:#111827
    classDef system fill:#F2F2F2,stroke:#828282,color:#111827

    class User,Cart,VerificationRequest,Notification,PasswordResetToken user
    class Session,Dish,Category,MealTemplate menu
    class Order,ChangeProposal order
    class Payment,WalletTransaction,RefundRequest payment
    class Setting system
```

## Compact Version

Use this version if draw.io renders the grouped/styled version too large.

```mermaid
flowchart LR
    User["User"]
    Session["Session"]
    Dish["Dish"]
    Category["Category"]
    MealTemplate["MealTemplate"]
    Cart["Cart"]
    Order["Order"]
    Payment["Payment"]
    WalletTransaction["WalletTransaction"]
    RefundRequest["RefundRequest"]
    VerificationRequest["VerificationRequest"]
    Setting["Setting"]
    Notification["Notification"]
    ChangeProposal["OrderItemChangeProposal"]
    PasswordResetToken["PasswordResetToken"]

    Session -->|"1 to *"| MealTemplate
    Session -->|"* to *"| Dish
    Dish -->|"* to 1"| Category
    MealTemplate -->|"* to 1"| Category
    User -->|"1 to 1"| Cart
    Order -->|"* to 1"| Session
    Order -->|"* to *"| Dish
    Order -->|"* to 0..1"| WalletTransaction
    Order -->|"* to 0..1"| MealTemplate
    Order -->|"1 to *"| ChangeProposal
    Payment -->|"1 to 0..1"| WalletTransaction
    WalletTransaction -->|"* to 1"| User
    Payment -->|"* to 1"| User
    RefundRequest -->|"0..1 to 0..1"| Order
    RefundRequest -->|"* to 1"| User
    RefundRequest -->|"0..1 to 1"| WalletTransaction
    ChangeProposal -->|"* to 1"| Order
    ChangeProposal -->|"* to 1"| User
    User -->|"1 to *"| Order
    User -->|"1 to *"| Notification
    User -->|"1 to *"| VerificationRequest
    User -->|"1 to *"| PasswordResetToken
    User -->|"1 to *"| ChangeProposal
    Setting -.->|"configures"| Session
    Setting -.->|"configures"| RefundRequest
```
