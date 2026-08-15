# SmartCanteen — Setup Guide

How to get the SmartCanteen backend (`SC.Api`) running, on a developer machine and on a server.

- [1. Prerequisites](#1-prerequisites)
- [2. Configuration files](#2-configuration-files)
- [3. Local setup](#3-local-setup)
- [4. Server setup — Ubuntu](#4-server-setup--ubuntu)
- [5. Server setup — Windows / IIS](#5-server-setup--windows--iis)
- [6. Configuration reference](#6-configuration-reference)
- [7. Troubleshooting](#7-troubleshooting)

---

## 1. Prerequisites

| Component | Version | Notes |
|---|---|---|
| .NET SDK | **10.0.100** (`rollForward: latestFeature`) | Pinned in [`global.json`](global.json). Runtime-only machines need the **ASP.NET Core Runtime 10.0**. |
| PostgreSQL | **17** | Snake-case naming via `EFCore.NamingConventions`; needs the `pgcrypto`-provided `gen_random_uuid()` (built into PG 13+). |
| Redis | **7** | Distributed cache (`AddStackExchangeRedisCache`). |
| Docker Engine + Compose plugin | latest | Only if you use the containerized path. |
| EF Core CLI | `dotnet tool install --global dotnet-ef` | Optional — migrations are applied automatically at startup. |

External services the API talks to (each can be left blank for basic local work, but the corresponding feature will fail):
Cloudinary (image + log upload), Resend (email), Google OAuth, Firebase Cloud Messaging (push), SePay (wallet top-up).

### 1.1 Operating system

| Target | Supported |
|---|---|
| Development | Linux, macOS, or Windows — anything the .NET 10 SDK runs on |
| Server (containers) | Any 64-bit Linux with Docker Engine; the images are `mcr.microsoft.com/dotnet/aspnet:10.0` (Debian-based), `postgres:17-alpine`, `redis:7-alpine` |
| Server (native) | Ubuntu 22.04/24.04 LTS with the ASP.NET Core Runtime 10.0, **or** Windows Server 2019+ with IIS and the .NET 10 Hosting Bundle |

Architecture: `x64` or `arm64`. The `Dockerfile` does not pin `--platform`, so it builds for the host's architecture. Only the Windows/IIS publish command in [§5.2](#52-publish) hardcodes `-r win-x64` — change it if you target `win-arm64`.

### 1.2 Hardware

These are **recommendations**, not measured production figures — this project has no load-test data. Sizing assumes a single campus canteen: hundreds of orders per session, a few concurrent managers, and a handful of robot/edge SignalR connections.

| Environment | CPU | RAM | Disk | Notes |
|---|---|---|---|---|
| Developer machine | 4 cores | 8 GB (16 GB comfortable) | 10 GB free | The SDK, NuGet cache, and a debug build of `SC.Api` alone are ~39 MB of output plus ~2 GB of SDK/packages. |
| Server — all-in-one Docker host | 2 cores | 4 GB | 20 GB + database growth | API + PostgreSQL + Redis + Caddy on one box. This is the shape [`Docker/docker-compose.yml`](Docker/docker-compose.yml) deploys. |
| Server — API only (DB/Redis managed elsewhere) | 2 cores | 2 GB | 10 GB | |

Why RAM is the binding constraint rather than CPU:

- **Five background services run continuously** in-process (`SessionFinalizationJob`, `ChangeProposalExpirationJob`, `OrderExpirationJob`, `ServingJobWatchdogJob`, `DailyLogUploadBackgroundService`), so the process is never idle even with no traffic.
- **SignalR holds long-lived connections** on three hubs (`/hubs/notifications`, `/hubs/robot`, `/hubs/visualization`) — each connected client costs memory for the lifetime of the connection.
- **Request/response bodies are buffered** by `ApiLoggerMiddleware` for the `ApplicationLogs` table, so large multipart uploads (refund evidence, verification documents) are held in memory during the request.

Disk grows from three places, none of which is capped by the app: the `ApplicationLogs` table, per-day/per-user Serilog files under `Logs/` (30 files retained per user per day), and normal database growth. Budget for log rotation on a long-lived server.

### 1.3 Network

| Direction | Requirement |
|---|---|
| Inbound | `443` (and `80` for the ACME challenge) if Caddy terminates TLS; the API itself listens on `8080` |
| Inbound — webhook | `POST /api/payments/sepay/ipn` must be **publicly reachable over HTTPS** for wallet top-ups to complete. It is `[AllowAnonymous]` and authenticated only by its HMAC signature. |
| Inbound — WebSockets | The reverse proxy must forward `Upgrade`/`Connection` headers, or SignalR degrades to long polling |
| Outbound | Cloudinary, Resend, Google (`oauth2.googleapis.com`), Firebase (`fcm.googleapis.com`), SePay — an air-gapped server can run the core app but not these features |
| Clock | NTP-synced. The SePay webhook rejects timestamps outside `WebhookTimestampToleranceSeconds` (default 300 s), so clock drift silently breaks top-ups. |

### 1.4 Database

Applied automatically on startup — see [§3.4](#34-database-migrations). Current schema: **47 migrations, 33 tables**. A fresh empty database is all that is required; no manual DDL, no seed data beyond the refund policies that ship inside the migrations.

---

## 2. Configuration files

`SC.Api/appsettings.json` and `SC.Api/appsettings.Development.json` are **git-ignored** ([.gitignore:18-20](.gitignore)) — they hold secrets and never land in the repo. You must create them yourself on every new machine and on the server.

Configuration is loaded in this order (later wins) — see [`SC.Api/Program.cs`](SC.Api/Program.cs):

1. `appsettings.json`
2. `appsettings.Notification.json` — **committed**, holds `NotificationRealtime`, `Firebase`, `NotificationTemplates`
3. `appsettings.{Environment}.json` (e.g. `appsettings.Development.json`)
4. Environment variables — nested keys use `__`, e.g. `ConnectionStrings__DefaultConnection`

### 2.1 Create `SC.Api/appsettings.json`

Copy this template and fill in the secrets:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=smartcanteen;Username=smartcanteen;Password=smartcanteen;Include Error Detail=true"
  },
  "Logging": {
    "MinimumLevel": "Information",
    "Database": {
      "TableName": "\"ApplicationLogs\""
    }
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"],
    "AllowedMethods": ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"],
    "AllowedHeaders": ["Content-Type", "Authorization"]
  },
  "RateLimit": {
    "PermitLimit": 100,
    "Window": 60,
    "QueueLimit": 0
  },
  "Jwt": {
    "Issuer": "SmartCanteen",
    "Audience": "SmartCanteenClients",
    "SecretKey": "<at least 32 random characters — REQUIRED>",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7,
    "EmailVerificationCodeMinutes": 5,
    "PasswordResetMinutes": 60,
    "BaseUrl": "http://localhost:5265"
  },
  "Frontend": {
    "BaseUrl": "http://localhost:3000",
    "VerifyEmailPath": "/verify-email",
    "ResetPasswordPath": "/reset-password"
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "smartcanteen:"
  },
  "Cloudinary": {
    "CloudName": "",
    "ApiKey": "",
    "ApiSecret": "",
    "ImagesFolder": "images",
    "LogsFolder": "logs"
  },
  "Resend": {
    "ApiKey": "",
    "FromEmail": "onboarding@resend.dev",
    "FromName": "SmartCanteen",
    "ApiBaseUrl": "https://api.resend.com"
  },
  "Google": {
    "ClientIds": []
  },
  "SePay": {
    "IpnUrl": "https://api.example.com/api/payments/sepay/ipn",
    "ApiKey": "",
    "SecretKey": "",
    "WebhookSecret": "",
    "WebhookTimestampToleranceSeconds": 300,
    "BankName": "",
    "BankAccountNumber": "",
    "BankAccountName": "",
    "PaymentCodePrefix": "SC",
    "RequiredTransferContentPrefix": "",
    "QrTemplate": "compact"
  },
  "Verification": {
    "FileSizeMaxBytes": 5242880,
    "AcceptedFormats": ["image/jpeg", "image/png", "application/pdf"],
    "EmailTokenHours": 24,
    "RequestExpiryDays": 14
  },
  "LogUpload": {
    "Enabled": true,
    "LocalLogsPath": "Logs",
    "UploadTimeUtc": "07:16:00"
  }
}
```

> **`ConnectionStrings:DefaultConnection` is a hard requirement** — startup throws `Connection string 'DefaultConnection' was not found.` without it.
>
> **`Jwt:SecretKey` fails quietly.** If it is blank the app still boots, but JWT bearer validation is registered without a signing key, so every `[Authorize]` endpoint returns `401` and no token you issue will ever validate. Always set it.

### 2.2 `appsettings.Notification.json`

Already committed. If it is ever missing, copy the example:

```bash
cp SC.Api/appsettings.Notification.example.json SC.Api/appsettings.Notification.json
```

`NotificationRealtime.HubPath` and `NotificationRealtime.ClientEventName` are validated with `ValidateOnStart()` — the API refuses to boot if they are blank or if `HubPath` doesn't start with a single `/`.

To enable push notifications set `Firebase.Enabled: true` plus either `ServiceAccountPath` (path to the service-account JSON) or `ServiceAccountJson` (the JSON inline).

---

## 3. Local setup

### 3.1 Option A — everything in Docker (fastest)

```bash
docker compose -f Docker/docker-compose.yml up -d --build
```

Brings up `postgres:17-alpine`, `redis:7-alpine` and the API. Compose overrides the connection string and Redis host with the container hostnames, so the API container talks to `postgres:5432` / `redis:6379`.

- API + Swagger: `http://localhost:8080/swagger`
- PostgreSQL: `localhost:5432` (`smartcanteen` / `smartcanteen` / db `smartcanteen`)
- Redis: `localhost:6379`

```bash
docker compose -f Docker/docker-compose.yml logs -f api     # follow logs
docker compose -f Docker/docker-compose.yml down            # stop
docker compose -f Docker/docker-compose.yml down -v         # stop + wipe the DB volume
```

The Dockerfile copies the whole repo into the image, so whatever `appsettings.json` exists at build time is baked in. Compose supplies the connection string and Redis host as environment variables, so the container starts even without `appsettings.json` — but with no `Jwt:SecretKey` every authenticated endpoint returns `401`. Create the file before building.

### 3.2 Option B — dependencies in Docker, API from the SDK

Best for day-to-day development (hot reload, breakpoints).

```bash
docker compose -f Docker/docker-compose.yml up -d postgres redis
```

```bash
dotnet restore SmartCanteen.slnx
```

```bash
dotnet run --project SC.Api
```

Uses the `http` launch profile: `http://localhost:5265` with `ASPNETCORE_ENVIRONMENT=Development`. The `https` profile also binds `https://localhost:7277`:

```bash
dotnet run --project SC.Api --launch-profile https
```

Swagger UI is served at `/swagger` in every environment.

### 3.3 Option C — fully local PostgreSQL + Redis

Create the database and role, then point `ConnectionStrings:DefaultConnection` at it:

```bash
sudo -u postgres psql -c "CREATE ROLE smartcanteen LOGIN PASSWORD 'smartcanteen';" -c "CREATE DATABASE smartcanteen OWNER smartcanteen;"
```

### 3.4 Database migrations

**Migrations are applied automatically on every startup** — `UseApiConfigurations()` calls `dbContext.Database.Migrate()` ([StartupConfigurations.cs:161](SC.Api/DependencyInjection/Configurations/StartupConfigurations.cs#L161)). A fresh, empty database is enough; the schema and the seeded refund policies are created on first run.

To apply them by hand instead (e.g. against a DB the app can't reach yet):

```bash
dotnet ef database update --project SC.Persistence --startup-project SC.Api
```

To add a migration after changing the model:

```bash
dotnet ef migrations add <Name> --project SC.Persistence --startup-project SC.Api
```

### 3.5 Optional seed data (robot serving demo)

The robot serving flow returns `204 No Content` from `next-job` unless trays and pickup slots exist. Seed the pool (idempotent — safe to re-run):

```bash
docker exec -i smartcanteen-postgres psql -U smartcanteen -d smartcanteen < seed_pool.sql
```

`seed_test_job.sql` creates a throwaway serving job for testing the robot loop.

### 3.6 Verify

```bash
curl -s http://localhost:5265/api/sessions | head -c 400
```

`GET /api/sessions` is `[AllowAnonymous]`, so it answers without a token — a JSON envelope back means DB + EF + the pipeline are all healthy. Then open `http://localhost:5265/swagger` and register a user via `POST /api/auth/...` to get a JWT.

### 3.7 Tests

```bash
dotnet test SC.Architecture.Test
```

Architecture conventions (layer dependencies, handler naming, sealed types) are enforced here — run it before pushing.

---

## 4. Server setup — Ubuntu

This is the path the repo's CD workflow already uses: source is shipped over SSH, then `docker compose up -d --build api` runs behind **Caddy**, which terminates TLS.

### 4.1 Install the host dependencies

```bash
sudo apt update && sudo apt install -y ca-certificates curl gnupg
```

```bash
curl -fsSL https://get.docker.com | sudo sh
```

```bash
sudo usermod -aG docker $USER && newgrp docker
```

Install Caddy (reverse proxy + automatic Let's Encrypt certificates):

```bash
sudo apt install -y debian-keyring debian-archive-keyring apt-transport-https && curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | sudo gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg && curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | sudo tee /etc/apt/sources.list.d/caddy-stable.list && sudo apt update && sudo apt install -y caddy
```

### 4.2 Deploy the application

```bash
sudo mkdir -p /opt/smartcanteen/api/develop && sudo chown -R $USER:$USER /opt/smartcanteen
```

Copy the source there (git clone, `scp` a `git archive` tarball, or let the CD workflow do it), then create the config file **inside `SC.Api/`**, next to `SC.Api.csproj` — it is git-ignored, so it never arrives with the source.

Write it with a **quoted** heredoc (`<<'JSON'`), so the shell leaves `$`, backticks and `\` in your secrets alone:

```bash
umask 077 && cat > /opt/smartcanteen/api/develop/SC.Api/appsettings.Development.json <<'JSON'
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=smartcanteen;Username=smartcanteen;Password=<db-password>;Include Error Detail=true"
  },
  "Jwt": {
    "Issuer": "SmartCanteen",
    "Audience": "SmartCanteenClients",
    "SecretKey": "<openssl rand -base64 48>",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7,
    "BaseUrl": "https://api.<your-domain>"
  },
  "Frontend": {
    "BaseUrl": "https://app.<your-domain>",
    "VerifyEmailPath": "/verify-email",
    "ResetPasswordPath": "/reset-password"
  },
  "Cors": {
    "AllowedOrigins": ["https://app.<your-domain>"],
    "AllowedMethods": ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"],
    "AllowedHeaders": ["Content-Type", "Authorization"]
  },
  "Redis": { "ConnectionString": "redis:6379", "InstanceName": "smartcanteen:" },
  "Cloudinary": { "CloudName": "", "ApiKey": "", "ApiSecret": "" },
  "Resend": { "ApiKey": "", "FromEmail": "no-reply@<your-domain>", "FromName": "SmartCanteen" },
  "Google": { "ClientIds": ["<client-id>.apps.googleusercontent.com"] },
  "SePay": {
    "IpnUrl": "https://api.<your-domain>/api/payments/sepay/ipn",
    "WebhookSecret": "<from the SePay dashboard>",
    "BankName": "",
    "BankAccountNumber": "",
    "BankAccountName": "",
    "PaymentCodePrefix": "SC"
  }
}
JSON
chmod 600 /opt/smartcanteen/api/develop/SC.Api/appsettings.Development.json
```

Full key reference in [§6](#6-configuration-reference); the complete template is in [§2.1](#21-create-scapiappsettingsjson). The values that must differ from your laptop:

- `ConnectionStrings:DefaultConnection` → `Host=postgres;...` (the compose **service name**, not `localhost`)
- `Redis:ConnectionString` → `redis:6379`
- `Jwt:SecretKey` → a fresh secret generated on the server, never the dev one
- `Jwt:BaseUrl` / `Frontend:BaseUrl` → the public HTTPS URLs
- `Cors:AllowedOrigins` → the frontend origin(s) only, never `*`
- `SePay:IpnUrl` → must be publicly reachable or top-ups never complete

Verify it parses before you build — a malformed file only surfaces as a startup crash:

```bash
python3 -m json.tool /opt/smartcanteen/api/develop/SC.Api/appsettings.Development.json > /dev/null && echo "valid JSON"
```

Then bring the stack up:

```bash
cd /opt/smartcanteen/api/develop/Docker && docker compose up -d --build
```

> **The file is baked into the image at build time.** `SC.Api/Dockerfile` does `COPY . .`, and `.dockerignore` does not exclude `appsettings*.json`, so the config is copied during `docker build`. Editing the file afterwards changes nothing until you rebuild — `docker compose restart api` will **not** pick it up. Always use `up -d --build`.
>
> To edit config without rebuilding, bind-mount it instead. Add to the `api` service in [`Docker/docker-compose.yml`](Docker/docker-compose.yml):
>
> ```yaml
>     volumes:
>       - ../SC.Api/appsettings.Development.json:/app/appsettings.Development.json:ro
> ```
>
> The app's content root is `/app`, so the mount lands exactly where the host expects it. Then `docker compose restart api` is enough.

### 4.2.1 The other two ways to get config onto the server

**Environment variables — no file at all.** Every key can be set as an env var; `:` becomes `__` and array items take an index. This is what compose already does for the connection string, and it keeps secrets out of the image entirely:

```bash
ConnectionStrings__DefaultConnection="Host=postgres;..."
Jwt__SecretKey="..."
Cors__AllowedOrigins__0="https://app.example.com"
```

**GitHub Actions — what [`.github/workflows/cd.yml`](.github/workflows/cd.yml) already does.** Store the whole file as the `APPSETTINGS_DEVELOPMENT_JSON` repository secret; the deploy step writes it under `umask 077` on every run:

```bash
printf '%s' "$APPSETTINGS_DEVELOPMENT_JSON" > "$TARGET_PATH/SC.Api/appsettings.Development.json"
```

If you use the CD pipeline, **do not** hand-write the file — the workflow does `rm -rf "$TARGET_PATH"` before extracting, so anything you placed there manually is destroyed on the next deploy. See [§4.5](#45-cicd-github-actions).

The API listens on `127.0.0.1:8080` from the host's point of view (compose publishes `8080:8080`).

> **Harden before going public:** the compose file ships development defaults — `ASPNETCORE_ENVIRONMENT: Development` and the `smartcanteen/smartcanteen` database password. For a real deployment set `ASPNETCORE_ENVIRONMENT: Production`, change `POSTGRES_PASSWORD` (and the matching connection string), and drop the `5432:5432` / `6379:6379` port publications so the database and cache are only reachable on the compose network.

### 4.3 Reverse proxy + TLS

Point DNS `A` records for `api.<domain>` and `app.<domain>` at the server, then install the repo's Caddyfile:

```bash
sudo cp /opt/smartcanteen/api/develop/Docker/Caddyfile /etc/caddy/Caddyfile && sudo systemctl reload caddy
```

[`Docker/Caddyfile`](Docker/Caddyfile) proxies `api.360retail.shop → localhost:8080` and `app.360retail.shop → localhost:3000` — edit the hostnames for your domain. Caddy obtains and renews certificates automatically and forwards `X-Forwarded-Proto` / `X-Forwarded-For`, which SignalR WebSockets need. Open only 80/443:

```bash
sudo ufw allow 80,443/tcp && sudo ufw enable
```

### 4.4 Alternative — systemd, no Docker

If you'd rather run the published binaries directly:

```bash
sudo apt install -y dotnet-sdk-10.0
```

```bash
dotnet publish SC.Api/SC.Api.csproj -c Release -o /var/www/smartcanteen
```

```ini
# /etc/systemd/system/smartcanteen-api.service
[Unit]
Description=SmartCanteen API
After=network.target postgresql.service redis-server.service

[Service]
WorkingDirectory=/var/www/smartcanteen
ExecStart=/usr/bin/dotnet /var/www/smartcanteen/SC.Api.dll
Restart=always
RestartSec=10
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:8080
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload && sudo systemctl enable --now smartcanteen-api && sudo journalctl -u smartcanteen-api -f
```

Serilog writes per-user rolling files under `WorkingDirectory/Logs`, so that directory must be writable by `www-data`.

### 4.5 CI/CD (GitHub Actions)

[`.github/workflows/cd.yml`](.github/workflows/cd.yml) deploys on every push to `develop`. Configure these repository/environment secrets (environment `development`):

| Secret | Purpose |
|---|---|
| `DEPLOY_HOST`, `DEPLOY_USER`, `DEPLOY_SSH_KEY`, `DEPLOY_PORT` | SSH target |
| `DEPLOY_PATH_API` | base path, default `/opt/smartcanteen/api` (the branch name is appended) |
| `DEPLOY_SIMULATE` | **defaults to `true`** — set it to `false` to actually deploy |
| `APPSETTINGS_DEVELOPMENT_JSON` | full contents of `appsettings.Development.json`, written with `umask 077` |
| `APPSETTINGS_NOTIFICATION` | full contents of `appsettings.Notification.json` |

The workflow wipes `$DEPLOY_PATH/$BRANCH` with `rm -rf` before extracting, so keep nothing else in that directory.

---

## 5. Server setup — Windows / IIS

### 5.1 Install

1. **IIS** — Server Manager → *Add Roles and Features* → **Web Server (IIS)**, including *WebSocket Protocol* (required by SignalR).
2. **[.NET 10 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0)** — installs the ASP.NET Core Runtime *and* the ASP.NET Core Module V2 (ANCM). Install it **after** IIS; if you install it first, repair it or run `iisreset` afterwards.
3. **PostgreSQL 17** and **Redis** — either native Windows services, containers, or a managed instance. Redis has no official Windows build; use WSL2/Docker or a hosted Redis.

```powershell
net stop was /y; net start w3svc
```

### 5.2 Publish

From a build machine with the .NET 10 SDK:

```powershell
dotnet publish SC.Api\SC.Api.csproj -c Release -r win-x64 --self-contained false -o C:\inetpub\SmartCanteen
```

Copy the output to the server, then place `appsettings.json` (and `appsettings.Production.json` if you use one) plus `appsettings.Notification.json` next to `SC.Api.dll`. Publishing generates `web.config` automatically; a minimal one looks like:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet"
                arguments=".\SC.Api.dll"
                stdoutLogEnabled="false"
                stdoutLogFile=".\logs\stdout"
                hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

### 5.3 Create the site

```powershell
Import-Module WebAdministration
New-WebAppPool -Name "SmartCanteenPool"
Set-ItemProperty IIS:\AppPools\SmartCanteenPool -Name managedRuntimeVersion -Value ""
Set-ItemProperty IIS:\AppPools\SmartCanteenPool -Name startMode -Value AlwaysRunning
New-Website -Name "SmartCanteen" -PhysicalPath "C:\inetpub\SmartCanteen" -ApplicationPool "SmartCanteenPool" -Port 8080
```

- The app pool **must** use *No Managed Code* (`managedRuntimeVersion = ""`) — ASP.NET Core is not hosted by the .NET Framework CLR.
- Set *Start Mode* to `AlwaysRunning` and the app pool *Idle Time-out* to `0`, otherwise IIS recycles the process and the background services (`SessionFinalizationJob`, `ChangeProposalExpirationJob`, `ServingJobWatchdogJob`, `DailyLogUploadBackgroundService`) stop running between requests.

### 5.4 Permissions

Serilog writes rolling per-user log files into `Logs\` under the content root:

```powershell
icacls "C:\inetpub\SmartCanteen\Logs" /grant "IIS AppPool\SmartCanteenPool:(OI)(CI)M" /T
```

Grant the same identity read access to the published folder, and keep `appsettings*.json` readable only by that identity plus administrators.

### 5.5 HTTPS and WebSockets

- Bind a certificate to port 443 in *IIS → Site → Bindings* (or run the site behind IIS ARR / another reverse proxy).
- Enable **WebSocket Protocol** on the server; without it SignalR silently falls back to long polling and the robot/notification hubs get much slower.
- The hubs accept the JWT from the `access_token` query parameter for any path under `/hubs` — make sure no URL-rewrite rule strips query strings there.

### 5.6 Verify

```powershell
curl.exe -s http://localhost:8080/api/sessions
```

Then check `Event Viewer → Windows Logs → Application` for ANCM entries if the site returns `500.30` / `500.31`.

---

## 6. Configuration reference

| Section | Key(s) | Required | Notes |
|---|---|---|---|
| `ConnectionStrings` | `DefaultConnection` | **Yes** | Npgsql connection string. Also used as the Serilog PostgreSQL sink target. |
| `Jwt` | `SecretKey` | **Yes in practice** | At least 32 chars. Blank does not throw — it silently makes every `[Authorize]` endpoint return `401`. Also `Issuer`, `Audience`, `AccessTokenMinutes` (15), `RefreshTokenDays` (7), `EmailVerificationCodeMinutes` (5), `PasswordResetMinutes` (60), `BaseUrl`. |
| `NotificationRealtime` | `HubPath`, `ClientEventName` | **Yes** | Validated on start. Lives in `appsettings.Notification.json`. |
| `Logging` | `MinimumLevel`, `Database.TableName` | No | Defaults `Information` / `"ApplicationLogs"`. Serilog writes to the console and to per-day/per-user rolling files under `Logs/` (30 files retained). Request/response records go to the `ApplicationLogs` table via `ApiLoggerMiddleware` → `ApiLogService`, created by EF migrations. |
| `Cors` | `AllowedOrigins`, `AllowedMethods`, `AllowedHeaders` | No | Empty means no cross-origin browser access. |
| `RateLimit` | `PermitLimit` (100), `Window` (60 s), `QueueLimit` (0) | No | Fixed-window limiter. |
| `Redis` | `ConnectionString`, `InstanceName` | Effectively yes | The distributed cache is registered unconditionally — a blank or wrong connection string fails when a cached endpoint is first hit, not at startup. |
| `Frontend` | `BaseUrl`, `VerifyEmailPath`, `ResetPasswordPath` | No | Used to build verification / password-reset links in emails. |
| `Cloudinary` | `CloudName`, `ApiKey`, `ApiSecret`, `ImagesFolder`, `LogsFolder` | No | Blank ⇒ image upload and the daily log upload are disabled. |
| `Resend` | `ApiKey`, `FromEmail`, `FromName`, `ApiBaseUrl` | No | Blank ⇒ no outbound email (verification codes won't be delivered). |
| `Google` | `ClientIds[]` | No | Every OAuth client that may sign in — web + one per Android signing key. |
| `Firebase` | `Enabled`, `ProjectId`, `ServiceAccountPath` \| `ServiceAccountJson` | No | Push notifications. In `appsettings.Notification.json`. |
| `SePay` | `WebhookSecret`, `BankName`, `BankAccountNumber`, `BankAccountName`, `PaymentCodePrefix`, `IpnUrl`, `WebhookTimestampToleranceSeconds`, `QrTemplate` | For top-ups | Top-up fails with a clear message if bank name/account or the code prefix are unset. `PaymentCodePrefix` must be 2–5 alphanumerics. |
| `Verification` | `FileSizeMaxBytes`, `AcceptedFormats[]`, `EmailTokenHours`, `RequestExpiryDays` | No | Identity-document upload limits. |
| `LogUpload` | `Enabled`, `LocalLogsPath`, `UploadTimeUtc` | No | Daily upload of local log files to Cloudinary. |
| `NotificationTemplates` | `Templates{}` | No | Title/message templates per event key. In `appsettings.Notification.json`. |

Three business settings live **in the database** (`Settings` table), not in JSON — `VND_PER_POINT`, `MIN_TOPUP_AMOUNT`, `MAX_TOPUP_AMOUNT`. Wallet top-up returns `Payment setting {code} is missing.` until they are present; manage them through `/api/settings`.

---

## 7. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `Connection string 'DefaultConnection' was not found.` at startup | `appsettings.json` missing or not copied next to the DLL. Remember it is git-ignored. |
| Every authenticated endpoint returns `401`, login "succeeds" but the token is rejected | `Jwt:SecretKey` is blank. The app boots anyway — set a secret of 32+ characters and restart. |
| Startup fails on `Notification realtime hub path and client event name must be configured.` | `appsettings.Notification.json` is missing or `HubPath`/`ClientEventName` are blank. |
| `Npgsql` cannot connect from the API container | Inside compose the host is the **service name** (`postgres`), not `localhost`. |
| Migrations don't run | They run automatically at startup; if the process dies first, check the console/`journalctl` output — a failed `Migrate()` throws before the web host starts listening. |
| `GET /api/robot/serving-jobs/next-job` always returns `204` | No trays or pickup slots exist — run `seed_pool.sql`. |
| Wallet top-up returns `Payment setting VND_PER_POINT is missing.` | Seed the three top-up settings in the `Settings` table. |
| SePay IPN returns `401` | `SePay:WebhookSecret` mismatch, missing `X-SePay-Signature` / `X-SePay-Timestamp` headers, or server clock drift beyond `WebhookTimestampToleranceSeconds` (300 s). Sync the clock with NTP. |
| SePay never calls back | `IpnUrl` must be publicly reachable over HTTPS and registered in the SePay dashboard. |
| SignalR clients keep reconnecting | Enable WebSockets (IIS) and make sure the reverse proxy forwards `Upgrade`/`Connection` headers — Caddy's `reverse_proxy` does this by default. |
| IIS returns `500.30` / `500.31` | Hosting Bundle not installed or installed before IIS, or the app pool is not *No Managed Code*. Reinstall the bundle and run `iisreset`. |
| Background jobs stop running on IIS | App pool idle time-out recycled the worker process — set *Idle Time-out* to `0` and *Start Mode* to `AlwaysRunning`. |

### Useful commands

```bash
docker compose -f Docker/docker-compose.yml logs -f api
```

```bash
docker exec -it smartcanteen-postgres psql -U smartcanteen -d smartcanteen
```

```bash
dotnet ef migrations list --project SC.Persistence --startup-project SC.Api
```

---

## Related docs

- [`README.md`](README.md) — project overview and architecture
- [`Docker/README.md`](Docker/README.md) — Docker stack details
- [`API-Modules/README.md`](API-Modules/README.md) — per-module API reference
- [`sequence-diagrams/`](sequence-diagrams/README.md) — per-controller sequence diagrams (controller → handler → database)
- [`AI/coding_convention.md`](AI/coding_convention.md) — coding conventions
