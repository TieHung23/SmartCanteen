# Database Review - 26042026_828

I reviewed the domain model against the SmartCanteen instruction and aligned the persistence layer to the DDD rule that value objects belong inside the aggregate root table, except for list value objects.

What I changed:

- Added EF Core and PostgreSQL package references to `SC.Persistence`.
- Added `SmartCanteenDbContext` so the persistence project can apply configuration classes from one place.
- Wired `SC.Api` to the persistence project and registered `SmartCanteenDbContext` against the existing `ConnectionStrings:DefaultConnection` setting.
- Added startup migration application so the database schema is updated automatically when the API starts.
- Added database configuration classes for `Category`, `User`, `Meal`, `Order`, and `Payment` under `SC.Persistence/Database/Configuration`.
- Updated the domain model so `Order` now tracks the selected `Meal` and a list of purchased `OrderItem` values.
- Added a new `Product` aggregate for purchasable menu items.
- Added `OrderItem` as a value object so each order line can store the product reference, quantity, and unit-price snapshot.
- Added persistence mappings for `Product` and for the owned `OrderItem` collection inside `Orders`.
- Mapped single value objects as owned types inside the aggregate root table:
  - `User.Balance` -> `Users`
  - `Meal.Money` -> `Meals`
  - `Payment.BalanceSnapshot` -> `Payments`
- `Product.Price` and `OrderItem.UnitPrice` are also mapped as owned value objects in their parent tables.
- Mapped the list value object `Meal.MealSettingsList` as an owned collection in its own table `MealSettings`.
- Ignored the navigation properties inside `MealSettings` during persistence mapping so the value object stays consistent with the aggregate ownership rule.

Domain note:

- `Order` now stores which `Meal` the student is ordering from and which `Product` entries were bought.
- `Product` is modeled as a separate aggregate root because it represents a purchasable menu item with its own stock and category data.

Design note:

- The domain model is generally consistent with the instruction.
- `MealSettings` is still modeled as a value object in the domain, but persistence now treats it as the special owned collection case required by the instruction.
- I did not change the domain classes because the structure already fits the intended DDD split once EF Core ownership is configured.
