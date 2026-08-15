# Sequence Diagrams

One file per controller: **`[controller]-diagrams.md`**. Every diagram traces the full path
**`Controller → IMediator → Handler → domain service / repository → SmartCanteenDbContext → PostgreSQL`**,
with the SQL each step issues and the guarded branch it can fail on.

| File | Controller | Route | Covers |
|---|---|---|---|
| [`sessions-diagrams.md`](sessions-diagrams.md) | `SessionsController` | `/api/sessions` | create session, manual finalize (+ finalize-now), auto-finalize job, session state |
| [`orders-diagrams.md`](orders-diagrams.md) | `OrdersController` | `/api/orders` | cart checkout, read scoping, order state |
| [`payments-diagrams.md`](payments-diagrams.md) | `PaymentsController` | `/api/payments` | top-up policy, QR creation, signed SePay IPN, idempotency, payment state |
| [`cart-diagrams.md`](cart-diagrams.md) | `CartController` | `/api/cart` | self-repairing `GET`, version-checked `PUT` / `DELETE`, cart-version state |
| [`changeproposals-diagrams.md`](changeproposals-diagrams.md) | `ChangeProposalsController` | `/api/changeproposals` | accept / request-refund / request-order-refund, the expiration job, proposal state |
| [`refunds-diagrams.md`](refunds-diagrams.md) | `RefundsController` | `/api/refunds` | student submit with evidence upload, read paths, refund state |
| [`refundmanager-diagrams.md`](refundmanager-diagrams.md) | `RefundManagerController` | `/api/manager/refunds` | approve (wallet credit), reject (unwind), queue + detail |

Diagram syntax: **PlantUML** (27 diagrams — 22 sequence, 5 state).

**Rendering.** GitHub does *not* render ```plantuml fences inline, so these show as code blocks on github.com. To view them:

- **IDE** — PlantUML integration plugin (JetBrains / Rider, already used in this repo) or the VS Code PlantUML extension; both preview the fenced blocks directly.
- **CLI** — extract a block and run `plantuml -tsvg diagram.puml` (needs Java + `plantuml.jar`).
- **Server** — paste into a PlantUML server, or run one locally: `docker run -d -p 8080:8080 plantuml/plantuml-server`.

Matches the existing PlantUML source at [`../AI/sequence_diagrams_smartcanteen.puml`](../AI/sequence_diagrams_smartcanteen.puml).

## The nine core write paths

Every one carries `autonumber` and activation bars:

| # | API | File |
|---|---|---|
| 1 | `POST /api/sessions/{id}/finalize` | [`sessions-diagrams.md`](sessions-diagrams.md) §2 |
| 2 | `POST /api/sessions` | [`sessions-diagrams.md`](sessions-diagrams.md) §1 |
| 3 | `POST /api/payments/top-up` | [`payments-diagrams.md`](payments-diagrams.md) §2 |
| 4 | `PUT /api/cart` | [`cart-diagrams.md`](cart-diagrams.md) §2 |
| 5 | `POST /api/orders` | [`orders-diagrams.md`](orders-diagrams.md) §1 |
| 6 | `POST /api/refunds` | [`refunds-diagrams.md`](refunds-diagrams.md) §1 |
| 7 | `POST /api/manager/refunds/{id}/approve` | [`refundmanager-diagrams.md`](refundmanager-diagrams.md) §1 |
| 8 | `POST /api/changeproposals/{id}/request-refund` | [`changeproposals-diagrams.md`](changeproposals-diagrams.md) §2 |
| 9 | `POST /api/changeproposals/{id}/accept` | [`changeproposals-diagrams.md`](changeproposals-diagrams.md) §1 |

Bars follow the call stack: a participant's bar opens when it is called and closes when it returns or when its caller moves on, so callers keep a long bar while leaf calls (repositories, the database) get short ones.

## Notation reference

[`uml-sequence-concepts.md`](uml-sequence-concepts.md) — what a sequence diagram is in UML 2.5.1: the metamodel, the full notation catalogue, the twelve combined-fragment operators, the PlantUML ↔ UML mapping, and a measured audit of where these diagrams deviate from the spec.

## Conventions used in these diagrams

| Element | Meaning |
|---|---|
| `autonumber` | every message is numbered sequentially, calls and returns alike (1, 2, 3 …) |
| `-> X ++` / `deactivate X` | **activation bar** (UML *ExecutionSpecification*) — the box on X's lifeline while X is doing work |
| `X -> Y` | **synchronous call** — solid line, filled arrowhead |
| `X -->> Y` | **reply** — dashed line, open arrowhead, per UML 2.5.1 |
| `X -->> Y --` | a reply that also closes X's activation bar |
| `critical` | a region that may not be interleaved — a row lock or a transaction |
| `actor` | a human or an external system |
| `database "PostgreSQL" as DB` | the database — messages to it are the **EF Core LINQ** the `DbSet` runs, never hand-written SQL |
| `->` / `-->` | a call / its return |
| `group <label>` | a named sub-step of the handler (validation, policy lookup, auto-credit) |
| `alt` / `else` | guarded branches, each ending in the HTTP status the controller returns |
| `opt` | conditional work that is skipped when the condition does not hold |
| `loop` | per-item work (per dish, per order item, per uploaded image) |
| trailing `note over` | how exceptions are handled and what status they map to — **not used in the nine write-path diagrams**, see below |

**The nine write-path diagrams are drawn compressed.** `POST /api/sessions`, `POST /api/sessions/{id}/finalize`, `POST /api/orders`, `PUT /api/cart`, `POST /api/payments/top-up`, `POST /api/refunds`, `POST /api/manager/refunds/{id}/approve`, `POST /api/changeproposals/{id}/accept` and `.../request-refund` elide the pass-through `IMediator` and `SmartCanteenDbContext` lifelines — a repository messages `DB` directly with the LINQ it runs — group closely related repositories onto one lifeline, and put consecutive fail-fast guards on a single multi-line message. They also carry no `autonumber`, no `loop` frames and no `note` frames: per-item work is written as a plain `check …` message and the `alt` under it routes the outcome `true` / `false`, and anything a note used to say is either folded into the message it annotated or stated in the prose around the diagram. The full chain is still named in each file's **Layers** line. Nothing about the order of operations, the guards or the transaction boundaries is dropped; the remaining diagrams keep every hop.

**Exception handling is uniform across those nine, so it is no longer drawn.** Any exception inside a write path rolls the transaction back and returns `Failure(ServerError)`, which the controller maps through `Error.HttpStatusCode` and otherwise to `400`. Two cases add a specific mapping: a `DbUpdateConcurrencyException` on a cart write becomes `CartVersionConflict` → `409`, and an `InvalidOperationException` raised by a domain method becomes `Failure(InvalidValue, ex.Message)` → `400`.

**Guards that cannot fire in practice are not drawn either.** A caller resolved from a valid JWT always exists, and an entity reached through a foreign key from a row that was just loaded always resolves — so "user not found", "refund request user not found" and "refund request order item not found" branches are omitted even though the handlers still check them defensively. Guards that a real request can trip — a wrong route id, a dish that was deactivated, an order that belongs to somebody else — stay.

**Authentication and authorization failures are not drawn.** `[Authorize]` / role checks are handled uniformly by the pipeline before the controller runs, so no diagram branches on a missing or invalid principal, a wrong role, or a `401` / `403`. Each file states the auth requirement in its header line instead. The one exception is the SePay IPN endpoint, which is `[AllowAnonymous]` — there the HMAC signature check *is* the endpoint's logic, not framework boilerplate, so it stays in the diagram.

**Queries are written as LINQ, not SQL.** Persistence goes through `GenericRepository<TEntity, TKey>`, so a `FindSingleAsync` is drawn as `FirstOrDefaultAsync`, `FindListAsync` as `ToListAsync`, `ExistsAsync` as `AnyAsync`, and eager loading as `Include` / `ThenInclude`. Writes are drawn as `SaveChangesAsync()` followed by the entity properties that change; transactions as `BeginTransactionAsync()` / `CommitAsync()` / `RollbackAsync()`.

**The five statements below are the only raw SQL in the system**, and they stay drawn as SQL because that is literally what the code sends — `FOR UPDATE` row locks, advisory locks and atomic `UPDATE … RETURNING` have no LINQ equivalent. ### Raw SQL vs LINQ

Messages written as **literal SQL** mean the code really does hand-write that SQL. Only three classes do, and only for locking and the guarded balance update:

| Class | Raw SQL it issues |
|---|---|
| `WalletDomainService` | `SELECT 1 ... FOR UPDATE`, and `UPDATE "Users" SET "Balance_Amount" = ... RETURNING "Balance_Amount"` (debit and credit) |
| `RefundLockService` | `SELECT 1 FROM "RefundRequests" ... FOR UPDATE`, `SELECT pg_advisory_xact_lock(hashtext(...))` |
| `SessionDishReservationService` | one reservation statement |

**Everything else is EF Core LINQ**, and is written that way in the diagrams — `Carts.FirstOrDefaultAsync(...)`, `repository.AddAsync(...)`, `SaveChangesAsync()`. Where EF emits an INSERT or UPDATE, the diagram names the LINQ/repository call, not the generated SQL.

Locks are drawn as explicit messages so concurrency is visible:

| Statement | Where |
|---|---|
| `SELECT 1 FROM "Users" WHERE "Id" = @userId FOR UPDATE` | `WalletDomainService.LockUserAsync` (ADO.NET command) |
| `UPDATE "Users" SET "Balance_Amount" = … - @amount … RETURNING "Balance_Amount"` | `WalletDomainService.TryDebitUserBalanceAsync` |
| `UPDATE "Users" SET "Balance_Amount" = … + @amount … RETURNING "Balance_Amount"` | `WalletDomainService.TryCreditUserBalanceAsync` |
| `SELECT 1 FROM "RefundRequests" WHERE "Id" = @id FOR UPDATE` | `RefundLockService.LockRefundRequestAsync` (`ExecuteSqlInterpolatedAsync`) |
| `SELECT pg_advisory_xact_lock(hashtext(@key))` | `RefundLockService.LockOrderRefundRequestsAsync` |

## Elsewhere

- [`../API-Modules/sequence-diagram-candidates.md`](../API-Modules/sequence-diagram-candidates.md) — the ranked backlog these files come from (robot serving lifecycle and pickup are still open)
- [`../API-Modules/README.md`](../API-Modules/README.md) — request/response reference per module
