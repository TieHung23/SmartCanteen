# Sequence Diagram Candidates — Full API Scan

Scan of the whole backend: **31 controllers + 3 SignalR hubs, 126 HTTP endpoints, 128 MediatR handlers, 5 background jobs.**

> Counts refreshed against the current tree. Since the original scan the repo gained one endpoint, one handler, and a fifth background job (`OrderExpirationJob`, 1-minute interval) — that job is **not** yet assessed in the tiers below.

Each endpoint was scored on the things that actually make a diagram worth drawing — not endpoint count:

| Signal | Why it matters |
|---|---|
| **Multi-actor** | more than one participant (student ↔ manager ↔ robot ↔ gateway ↔ background job) |
| **State machine** | moves an aggregate between statuses with guarded transitions |
| **Money** | debits/credits the wallet, creates refunds, or is gated by a refund policy |
| **Transaction + lock** | `BeginTransactionAsync` + row lock, i.e. concurrency is part of the contract |
| **Fan-out after commit** | notifications / SignalR / chained commands whose failure must not roll back |
| **Branching** | the number of distinct guarded failure paths a reader has to keep in their head |

Metrics below are measured (non-blank handler LOC, constructor dependencies, guarded failure branches).

---

## Already documented

[`../sequence-diagrams/`](../sequence-diagrams/README.md) — one file per controller, each tracing `Controller → Handler → Database`:

| File | Covers |
|---|---|
| [`sessions-diagrams.md`](../sequence-diagrams/sessions-diagrams.md) | create session, manual finalize (+ finalize-now), auto-finalize job |
| [`orders-diagrams.md`](../sequence-diagrams/orders-diagrams.md) | cart checkout, read scoping, order lifecycle |
| [`payments-diagrams.md`](../sequence-diagrams/payments-diagrams.md) | SePay top-up, signed IPN, idempotency |
| [`cart-diagrams.md`](../sequence-diagrams/cart-diagrams.md) | **A5** — self-repairing `GET`, version-checked writes |
| [`changeproposals-diagrams.md`](../sequence-diagrams/changeproposals-diagrams.md) | **A3** — accept / item refund / order refund, expiration job |
| [`refunds-diagrams.md`](../sequence-diagrams/refunds-diagrams.md) · [`refundmanager-diagrams.md`](../sequence-diagrams/refundmanager-diagrams.md) | **A4** — submit, approve, reject |

Still open: **A1** (robot serving lifecycle) and **A2** (pickup completion & no-show).

---

## Tier A — draw these next

Highest payoff: multi-actor, state-machine-driven, or money-moving. Ordered by value.

### A1. Robot serving lifecycle (4 entry points, one story)

The single biggest undocumented area, and the only one where the actors are a **physical robot and an edge controller** — hardest thing in the system to reason about from code alone.

| Entry point | Handler | LOC | Deps | What makes it complex |
|---|---|---|---|---|
| `POST /api/robot/serving-jobs/next`<br>+ hub `VisualizationHub.PullNextJob` | `PullNextJobCommandHandler` | 139 | **19** | FIFO over queued jobs, filtered to sessions whose window is open *right now*; cancels the job if the order vanished; resolves dish → lane code + arm station from the session's slot configuration; **resumes** a requeued job by replaying `RobotEventLogs` for `PlaceCompleted` so already-picked dishes are skipped; claims `Queued → Pushed` |
| Hub `RobotHub.ReportStatus` / `VisualizationHub.ReportStatus` | `ReportServingStatusCommandHandler` | 235 | **18** | Maps robot state strings to `RobotEventType`; drives `Pushed → Assembling` / `→ Failed`; resolves "which arm" from the reported station and piggybacks a **heartbeat** on it; syncs `Order.Pending → Preparing` with history; fans out student + staff notifications after commit |
| `POST /api/robot/serving-jobs/{id}/bind-tray`<br>+ hub `BindTray` / `AutoBindTray` | `BindTrayCommandHandler` (80/7), `AutoBindTrayCommandHandler` (72/7) | | | Binds the **physically scanned** tray to the job — deliberately not auto-assigned at pull time so `TrayId` always matches the real tray |
| `POST /api/pickup/assign` | `AssignPickupSlotCommandHandler` | 132 | 16 | Cross-validates slot is `Empty`, job is `Assembling` (not Queued/Pushed/Failed), and the scanned tray **is** the job's tray; then assigns the slot, **releases the tray back to the pool**, `job.MarkOnShelf`, `Order → ReadyForPickup` + history, best-effort "come collect" notification |

> Suggested output: **one lifecycle diagram** (order → job queued → pulled → tray bound → assembling → shelved → collected) plus a detail diagram for `PullNextJob`'s resume logic. This is where a diagram replaces the most reading.

### A2. Pickup completion & no-show

| Endpoint | Handler | LOC | Deps | Why |
|---|---|---|---|---|
| `POST /api/pickup/collect` | `CollectOrderCommandHandler` | 108 | 13 | `ReadyForPickup → Completed`, closes the job, frees the slot, realtime + history |
| `PATCH /api/manager/pickup-slots/{id}/force-clear` | `ForceClearPickupSlotCommandHandler` | 99 | 12 | No-show path: only clears an order that is still `ReadyForPickup`, marks it `Expired` with a `PickupExpiredForceClear` history reason, frees the slot |

Worth pairing in one diagram — they are the two exits from the same state.

### A3. Change proposal resolution (3 student choices + a timeout)

After Manual Finalize raises proposals, the student picks one of three actions and a background job decides for anyone who doesn't answer. `change-proposal-refund-flow.md` describes this in prose; it has **no diagram**.

| Endpoint | Handler | LOC | Deps | Branches |
|---|---|---|---|---|
| `POST /api/changeproposals/{id}/request-order-refund` | `RequestOrderRefundFromProposalCommandHandler` | **279** | **18** | 15 — **the largest handler in the codebase**: ownership + expiry + order-status guards, refund row lock, duplicate-refund guard, policy resolution from settings (rejects policies that require images), amount computed excluding already-refunded items, auto-credit, notify |
| `POST /api/changeproposals/{id}/request-refund` | `RequestRefundFromProposalCommandHandler` | 237 | 15 | 16 — same shape at item level |
| `POST /api/changeproposals/{id}/accept` | `AcceptChangeProposalCommandHandler` | 176 | 11 | 16 — swaps the dish; must re-validate the replacement against the session, the order's meal template and the required-category rule |
| *(background)* `ChangeProposalExpirationService` | — | 460 | — | Splits **required-item timeout** (auto full-order refund + auto-credit) from **optional-item timeout**; per-proposal transaction so one failure doesn't poison the sweep |

### A4. Refund request lifecycle

| Endpoint | Handler | LOC | Deps | Why |
|---|---|---|---|---|
| `POST /api/refunds` | `SubmitRefundRequestCommandHandler` | 204 | 12 | 9 guarded rejections: eligibility window, duplicate active request, policy lookup by scope/code from `Settings`, `RequiresImage` enforcement, amount computation |
| `POST /api/manager/refunds/{id}/approve` | `ApproveRefundRequestCommandHandler` | 157 | 14 | Locks the user, credits the wallet, writes a `WalletTransaction`, flips item/order statuses, notifies |
| `POST /api/manager/refunds/{id}/reject` | `RejectRefundRequestCommandHandler` | 166 | 13 | Restores item statuses and writes order status history |

One diagram covering submit → approve/reject, with the auto-credit path from A3 shown as an alternative entry, would cover the whole money-out story.

### A5. Cart — the self-healing read

`GET /api/cart` is **not** a plain read and is the most commonly misread endpoint in the codebase.

| Endpoint | Handler | LOC | Deps | Why |
|---|---|---|---|---|
| `GET /api/cart` | `GetCartQueryHandler` | 139 | 10 | Drops sessions that expired/closed since the cart was written, enriches items with live dish data, and if anything was dropped **opens a transaction, locks the user, re-reads the cart and only writes back if the version still matches** — otherwise it rolls back and returns the other writer's cart |
| `PUT /api/cart` | `UpdateCartCommandHandler` | 93 | 7 | Version-checked write under the same user lock, full meal-template validation before persisting |

---

## Tier B — worth a diagram, lower priority

| Endpoint | Handler | LOC / Deps / Branches | Why it is more than CRUD |
|---|---|---|---|
| `PUT /api/sessions/{id}` | `UpdateSessionCommandHandler` | 200 / 11 / 14 | Most branch-heavy non-refund handler — guards against editing a finalized session, shrinking a window that already has orders, and template/dish edits that would invalidate existing orders |
| `POST /api/auth/google` | `GoogleLoginCommandHandler` | 129 / 9 / 12 | Validates the Google ID token against every configured client ID, then **provisions or links** a local account, then issues the token pair |
| `POST /api/auth/login` → `refresh` → `logout` | 90 / 91 / 49 LOC | | Refresh-token rotation and revocation is a three-endpoint state machine that a single diagram explains far better than three doc sections |
| `POST /api/auth/register` → `verify-email` | 87 / 60 LOC | | Registration, code issuance via Resend, verification, and the `verified` claim gate |
| `POST /api/auth/forgot-password` → `reset-password` | 76 / 68 LOC | | Token issuance → FE deep link → consumption; two actors and an email hop |
| `POST /api/verification/submit` | `SubmitVerificationCommandHandler` | 108 / 11 | Per-file size/format validation → **Cloudinary upload** → transactional request creation → manager notification. Has an external-service hop that can fail mid-flow |
| `POST /api/admin/verifications/{id}/approve` \| `/reject` | 90 / 88 LOC | | Grants the `verified` claim that gates other endpoints — the payoff of the flow above |
| `PUT /api/manager/orders/{id}/status` | `UpdateOrderCommandHandler` | 100 / 8 | Explicitly **blocks** manual transitions into serving-flow statuses (`ReadyForPickup`, etc.) so it can't collide with the robot pipeline — a rule worth showing next to A1 |
| `POST /api/manager/serving-jobs/{id}/requeue` \| `/manual-complete` | 54 / 63 LOC | | Staff escape hatches out of a stuck robot job; only meaningful drawn against the A1 lifecycle |
| `POST /api/manager/users/{id}/suspend` \| `ban` \| `reactivate` | `UpdateUserAccountStatusCommandHandler` | 96 / 7 / 7 | One handler, three routes, guarded transitions between account states |
| `PATCH /api/manager/trays/{id}/force-release` | 79 / 8 | | Manual override of tray pool state — pairs with A1 |

### Background jobs (no HTTP entry point, still worth diagrams)

| Job | Service | LOC | Why |
|---|---|---|---|
| `ServingJobWatchdogJob` | `ServingJobWatchdogService` | 142 | Requeues stuck `Pushed`/`Assembling` jobs up to 3 times then fails them, **but skips jobs whose dishes are all placed** (waiting on staff, not stuck); separately flips arms with a stale heartbeat to `Offline`; notifies staff after commit |
| `ChangeProposalExpirationJob` | `ChangeProposalExpirationService` | 460 | See A3 |
| `SessionFinalizationJob` | `FinalizeSessionService.AutoFinalizeOverdueSessionsAsync` | — | ✅ already drawn |
| `DailyLogUploadBackgroundService` | — | 125 | Scheduled Cloudinary upload of rolling log files — low value, prose is enough |

---

## Tier C — skip, plain CRUD

These are `validate → repository → save`, single actor, no state machine, no money. A sequence diagram would just restate the controller signature.

Categories · Dishes · Settings · Refund policies (manager CRUD) · Trays create/retire · Robot arms CRUD · Slot configurations CRUD · Shelf stocks create/refill · Pickup slots create/retire · Device token register/unregister · Notification list / mark-read / delete · Admin create-notification · Wallet transaction list · Admin log list/detail · All `GET` list/detail endpoints for sessions, orders, refunds, verifications.

**One caveat on reports.** `GetReportSummary` (355 LOC) and `GetSessionDetailReport` (320 LOC) are the two longest handlers in the project, but they are read-only aggregation — a single actor and no state change. A **sequence** diagram adds nothing; if they need documenting, a data-lineage table (which entities feed which metric) is the right format.

---

## Suggested order of work

1. **A1 robot serving lifecycle** — biggest gap, hardest to read from code, involves hardware
2. **A3 change proposal resolution** — largest handlers in the codebase, prose-only today
3. **A4 refund lifecycle** — money movement, and it is the destination of several A3 paths
4. **A2 pickup completion & no-show** — small, closes out the A1 story
5. **A5 cart** — short, and corrects a common misreading of `GET /api/cart`
6. Tier B as needed, starting with auth (B: login/refresh/logout) since every FE integration touches it
