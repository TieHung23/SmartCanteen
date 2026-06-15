# SmartCanteen — System Architecture Diagram

> Frontend (Next.js) → Backend (ASP.NET on Ubuntu) → PostgreSQL, all containerised with Docker.

```mermaid
graph TB
    subgraph "🧑 User"
        Browser["🌐 Web Browser<br/>(Desktop / Mobile)"]
    end

    subgraph "🐳 Docker Host — Ubuntu Server"
        subgraph "Frontend Container"
            NextJS["⚛️ Next.js App<br/>(Port 3000)"]
        end

        subgraph "Backend Container"
            API["🚀 ASP.NET Core API<br/>(Port 5000)"]
        end

        subgraph "Database Container"
            PostgreSQL[("🐘 PostgreSQL<br/>(Port 5432)")]
            Redis[("⚡ Redis Cache<br/>(Port 6379)")]
        end
    end

    subgraph "☁️ External Services"
        Cloudinary["Cloudinary<br/>(Image Storage)"]
        PaymentGW["Payment Gateway<br/>(SePay / Momo / VnPay)"]
    end

    Browser -->|"HTTPS"| NextJS
    NextJS -->|"HTTP / JSON &amp; multipart/form-data"| API
    API -->|"image upload"| Cloudinary
    API --> PostgreSQL
    API --> Redis
    API --> PaymentGW
```

## Docker Compose Layout

```yaml
services:
  postgres:
    image: postgres:16
    ports: ["5432:5432"]
    volumes: ["pgdata:/var/lib/postgresql/data"]

  redis:
    image: redis:7
    ports: ["6379:6379"]

  backend:
    build: ./SC.Api
    ports: ["5000:8080"]
    depends_on: [postgres, redis]
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;...

  frontend:
    build: ./frontend
    ports: ["3000:3000"]
    depends_on: [backend]
```

## Data Flow

| Step | From | To | Description |
|------|------|----|-------------|
| 1 | Browser | Next.js | User visits app, SSR renders page |
| 2 | Next.js | API | API calls with JWT Bearer token |
| 3 | API | PostgreSQL | CRUD via EF Core / Repository |
| 4 | API | Redis | Cache lookups, session data |
| 5 | API | Cloudinary | Upload image to Cloudinary, save returned URL |
| 6 | API | Payment GW | Create top-up, handle SePay IPN webhook |
