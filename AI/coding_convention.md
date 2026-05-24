# SmartCanteen Coding Conventions

## Architecture & Project Organization

**Layered Architecture (DDD - Domain-Driven Design):**

- **SC.Api**: ASP.NET Core API entrypoint and HTTP endpoints
- **SC.Application**: Application use cases and orchestration (MediatR queries/commands)
- **SC.Domain**: Domain entities, value objects, aggregates, and business rules
- **SC.Contract**: Shared contracts, result/error models
- **SC.Infrastructure**: Infrastructure integrations (external services)
- **SC.Persistence**: Data access and EF Core configurations
- **SC.Architecture.Test**: Architecture and test-related checks

---

## Namespace Structure

```
SC.[Project].[Domain].[Subdomain].[Stereotype]
```

Examples:

- `SC.Domain.Domain.Meal.AggregateRoot` → Meal aggregate
- `SC.Domain.Domain.Meal.ValueObject` → Meal value objects
- `SC.Domain.Abstraction.Entities` → Base abstractions
- `SC.Application.MediatR.Category.GetAllCategories` → Application handlers

---

## Naming Conventions

| Scope                | Convention     | Example                                                 |
| -------------------- | -------------- | ------------------------------------------------------- |
| **Classes**          | PascalCase     | `CategoriesController`, `MealSettings`, `Money`         |
| **Interfaces**       | I + PascalCase | `IMediator`, `IRepositoriesBase`, `ICurrentUserService` |
| **Properties**       | PascalCase     | `Name`, `Id`, `CreatedAtUtc`, `BalanceAmount`           |
| **Local variables**  | camelCase      | `mediator`, `request`, `result`                         |
| **Enums**            | PascalCase     | `OrderStatus`, `PaymentMethod`, `Role`                  |
| **Database columns** | snake_case     | `login_id`, `api_method`, `created_date`                |

---

## DDD Patterns

### Aggregate Roots

- Main business entities: `User`, `Meal`, `Dish`, `Order`, `Payment`, `Category`, `Setting`, `VerificationRequest`, `ApiLog`
- Located in: `Domain/[Feature]/AggregateRoot/`

### Value Objects

- Immutable objects: `Money`, `MealSettings`, `OrderItem`, `BalanceSnapshot`, `VerificationDocument`
- Located in: `Domain/[Feature]/ValueObject/`

### Enums

- `Role` (Admin=1, Manager=2, User=3)
- `UserCategory` (Student=1, Lecturer=2, Staff=3, External=4)
- `OrderStatus`, `PaymentStatus`, `PaymentMethod`, `PaymentType`
- Located in: `Domain/[Feature]/Enum/`

### Base Classes

```csharp
AggregateRoot   // Base for aggregate roots
ValueObject     // Base for value objects
Entity          // Base entity
IAuditableEntity // Tracking: CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy
```

---

## API Conventions

### Controllers

- Route: `[Route("api/[controller]")]`
- Versioning: `[ApiVersion("1.0")]`
- Use MediatR for requests: `IMediator.Send()`
- Pattern: Query → Handler → Result

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class CategoriesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllCategories(
        [FromQuery] GetAllCategoriesQuery request)
    {
        var result = await mediator.Send(request);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }
}
```

---

## Database Conventions

### Entity Framework Core + PostgreSQL

- Snake_case column naming: `LoginId` → `login_id`
- Owned types for value objects in same table: `User.Balance` → `balance_amount`, `balance_currency`
- Owned collections for list value objects: `Meal.MealSettingsList` → separate `meal_settings` table
- Auditable fields (all aggregates): `created_at_utc`, `updated_at_utc`, `created_by`, `updated_by`
- Primary key: `Guid Id`

### Key Tables

- `users`
- `categories`
- `meals`, `meal_settings`
- `dishes`
- `orders`, `order_items`
- `payments`
- `settings`
- `verification_requests`, `verification_documents`
- `application_apilog`

---

## Logging Conventions

### Dual Logging Setup

1. **DB Logging** → `ApiLog` table via EF Core
   - Columns: `login_id`, `log_level`, `api_url`, `api_method`, `message`, `error_trace`, `request_id`, etc.

2. **File Logging** → Per-user rolling files via Serilog
   - NuGet: `Serilog`, `Serilog.Sinks.File`, `Serilog.Sinks.Map`

### Log Levels

```csharp
public enum AppLogLevel
{
    ERROR = 0,
    INFO = 1,
    DEBUG = 2,
}
```

---

## Code Style & C# Features

- **C# Version**: 12+ (latest .NET 8/9)
- **Pattern**: Primary constructors: `public class Controller(IMediator mediator)`
- **File-scoped namespaces**: `namespace SC.Api.Controllers;`
- **Records for DTOs**: Use where immutability is needed
- **Property initialization**: Auto-properties with init accessors
- **Nullable reference types**: Enabled by default
- **null-forgiving operator**: Use sparingly

---

## Working Principles

1. **Prioritize facts** from source code over assumptions
2. **Keep naming consistent** with existing domain language
3. **Don't invent fields** that aren't in source code
4. **Prefer minimal changes** — avoid unrelated refactors
5. **Call out assumptions** explicitly when uncertain

---

## Project Structure by Domain

```
SC.Domain/
├── Abstraction/           (Base classes & interfaces)
│   ├── Aggregates/
│   ├── Entities/
│   ├── Repositories/
│   └── Services/
├── Domain/                (Business aggregates)
│   ├── Category/
│   ├── Dish/
│   ├── Meal/
│   ├── Order/
│   ├── Payment/
│   ├── User/
│   ├── Verification/
│   └── Logging/
└── SharedKernel/          (Cross-cutting value objects)
    ├── Enums/
    └── ValueObjects/
```

---

## Technology Stack

- **Runtime**: .NET 8/9
- **Web Framework**: ASP.NET Core
- **API Versioning**: Asp.Versioning
- **Mediator Pattern**: MediatR
- **ORM**: Entity Framework Core
- **Database**: PostgreSQL (Npgsql)
- **Logging**: Serilog
- **Architecture**: Clean Architecture / DDD
