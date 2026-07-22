# 🍱 SmartCanteen

A smart canteen management and meal-ordering platform built for the **FPT University** ecosystem — bridging canteen administrative planning and student dietary preferences with structured yet customizable meal selection, wallet-based payments, and **robot-served pickup**.

## 📖 Overview

SmartCanteen lets canteen managers define meal sessions with selection rules (e.g. a plate must contain exactly **2 meats, 1 fish, and rice or noodles**), while students build a custom plate that satisfies those rules, pay from a point wallet, and pick up their meal from a slot served by a **FAIRINO FR3 robot arm**.

### Key Modules

| Module | Description |
|--------|-------------|
| 🔐 **Auth & Users** | Registration, login (JWT + Google OAuth), email verification, identity verification with document review |
| 🍽️ **Menu & Ordering** | Dish categories, meal sessions with selection constraints, cart, orders |
| 💳 **Payment** | Point wallet, top-ups via SePay / Momo / ZaloPay / VnPay, refunds with configurable refund policies |
| 🤖 **Robot Serving** | FAIRINO FR3 integration, serving jobs, tray/shelf/pickup-slot management |
| 🔔 **Notification** | Real-time via SignalR + Firebase Cloud Messaging |
| 📊 **Reports & Logs** | Manager reports, admin audit logs, dual logging (DB + per-user files) |

### System Roles

- **Canteen Manager** — configures sessions and selection constraints, manages the daily menu, monitors order volume for kitchen portion control, reviews verification requests and refunds.
- **Student / User** — browses the session menu, builds a plate that satisfies the session rules, pays with wallet points, tracks order history and pickup status.
- **Admin** — user management, system settings, notifications, and audit logs.

## 🛠️ Tech Stack

| Category | Technology |
|----------|-----------|
| Runtime | .NET 10 / C# 13 |
| Web Framework | ASP.NET Core (REST, versioned via `Asp.Versioning`) |
| Architecture | Clean Architecture + Domain-Driven Design (CQRS with MediatR) |
| Database | PostgreSQL 17 + Entity Framework Core (Npgsql, snake_case naming) |
| Cache | Redis |
| Real-time | SignalR, Firebase Cloud Messaging |
| Auth | JWT Bearer, Google OAuth, BCrypt |
| Payments | SePay, Momo, ZaloPay, VnPay |
| Media Storage | Cloudinary |
| Email | Resend |
| Logging | Serilog (console, rolling files, PostgreSQL) + OpenTelemetry |
| Validation | FluentValidation |
| Testing | xUnit + NetArchTest (architecture conventions enforced by tests) |
| Deployment | Docker Compose (API + PostgreSQL + Redis), Caddy reverse proxy |

## 🏗️ Architecture

Clean Architecture with strict layer separation, enforced by automated architecture tests:

```
SC.Api                 → Controllers, SignalR hubs, middleware (presentation)
SC.Application         → CQRS handlers (MediatR), validators, use cases
SC.Domain              → Aggregates, value objects, enums, repository interfaces
SC.Contract            → ICommand/IQuery, Result envelope, shared contracts
SC.Infrastructure      → JWT, Cloudinary, Firebase, Redis, payment gateways
SC.Persistence         → EF Core DbContext, migrations, repositories, UnitOfWork
SC.Architecture.Test   → NetArchTest conventions + validation tests
```

**Request flow:** `Controller → IMediator.Send() → Command/Query Handler → Domain/Repository → Result<T>`

Every API response uses a consistent envelope:

```json
{
  "value": { },
  "isSuccess": true,
  "message": "string",
  "error": null
}
```

## 🔄 Core Flows

### 1. Session Creation (Manager)

Manager creates a meal session containing basic info (name, description, time window with order cutoff), the list of dishes available in the session, and **MealTemplates** — the rule-sets defining how many items may be selected per category.

### 2. Meal Customization & Ordering (Student)

```
Student → View session menu → Build plate (validated against MealTemplate rules)
        → Add to cart (rejected unless constraints are satisfied)
        → Checkout → Pay with wallet points → Order confirmed
```

Out-of-stock dishes are hidden/disabled in real time; orders must be placed before the session cutoff so the kitchen can prepare exact portions.

### 3. Robot Serving & Pickup

Confirmed orders become **serving jobs**: the FAIRINO FR3 robot arm places trays into pickup slots, shelf stock is tracked, and the student is notified when their meal is ready for pickup.

### 4. Identity Verification

User submits a verification request with document photos → Manager reviews and approves/rejects → approved users gain the `verified` claim required for protected features.

### 5. Refunds & Change Proposals

Order changes go through **change proposals**; approved changes can trigger refunds to the point wallet, governed by configurable refund policies.

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (see `global.json`)
- Docker Engine + Docker Compose (for the containerized stack)
- PostgreSQL 17 (if running locally without Docker)

### Run with Docker

```bash
docker compose -f Docker/docker-compose.yml up -d --build
```

- API: `http://localhost:8080`
- PostgreSQL: `localhost:5432`

### Run Locally

```bash
dotnet build SmartCanteen.slnx
dotnet run --project SC.Api        # http://localhost:5265 (Swagger UI enabled)
```

Configure the database connection in `SC.Api/appsettings.Development.json` (`ConnectionStrings:DefaultConnection`), then apply migrations:

```bash
dotnet ef database update --project SC.Persistence --startup-project SC.Api
```

### Run Tests

```bash
dotnet test SC.Architecture.Test
```

## 📚 Documentation

| Doc | Content |
|-----|---------|
| [`API-Modules/`](API-Modules/README.md) | Per-module API reference (requests, responses, routes) |
| [`Flow/`](Flow/) | Frontend integration flows with sequence diagrams |
| [`AI/coding_convention.md`](AI/coding_convention.md) | Coding conventions and DDD patterns |
| [`DDD_ANALYSIS.md`](DDD_ANALYSIS.md) | Architecture analysis |
| [`Docker/README.md`](Docker/README.md) | Docker deployment guide |
