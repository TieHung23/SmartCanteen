# Activity-bar corrections — the 9 core APIs

Corrected copies of the nine main flows. The originals in [`../sequence-diagrams/`](../sequence-diagrams/) are untouched.

| # | API | File | Fixes |
|---|---|---|---|
| 1 | `POST /api/sessions/{id}/finalize` | [`01-sessions-finalize.md`](01-sessions-finalize.md) | 2 |
| 2 | `POST /api/sessions` | [`02-sessions-create.md`](02-sessions-create.md) | 1 |
| 3 | `POST /api/payments/top-up` | [`03-payments-top-up.md`](03-payments-top-up.md) | 1 |
| 4 | `PUT /api/cart` | [`04-cart-update.md`](04-cart-update.md) | 3 |
| 5 | `POST /api/orders` | [`05-orders-checkout.md`](05-orders-checkout.md) | 3 |
| 6 | `POST /api/refunds` | [`06-refunds-submit.md`](06-refunds-submit.md) | 2 |
| 7 | `POST /api/manager/refunds/{id}/approve` | [`07-refunds-approve.md`](07-refunds-approve.md) | 4 |
| 8 | `POST /api/changeproposals/{id}/request-refund` | [`08-changeproposal-item-refund.md`](08-changeproposal-item-refund.md) | 4 |
| 9 | `POST /api/changeproposals/{id}/accept` | [`09-changeproposal-accept.md`](09-changeproposal-accept.md) | 2 |

## The two defects

**1. DB bars closed without a return.** Every database call in these flows is `await`ed, so control comes back to the caller — but many bars were closed with a bare `deactivate DB` instead of a reply arrow. A bar that ends with no return reads as fire-and-forget. It matters most on the locks: `SELECT 1 ... FOR UPDATE` and `SELECT pg_advisory_xact_lock(...)` **block until granted**, which is the whole point of drawing them, and that blocking was invisible.

**2. The notification fan-out replied to the wrong lifeline.** `H -> NOTI` followed by `NOTI -->> STU --` made the notification service *reply* to the student, who never called it. A reply must answer the call that invoked it. Corrected to: `NOTI -->> H --: queued` (the real reply), then `NOTI ->> STU:` as an **asynchronous** message — solid line, open arrowhead, no reply and no bar, which is what a SignalR/FCM push actually is.

Everything else — `opt` guards, `critical` regions, `ref` blocks, numbering, participant boxes — is unchanged from the originals.
