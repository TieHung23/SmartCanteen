# SmartCanteen Session API

Base URL: `/api/sessions`  
API Version: `1.0`

Auth: `GET` endpoints are `[AllowAnonymous]`. All other endpoints require `[Authorize]`.

AutoFinalizePolicy: `0=AutoReject, 1=AutoConfirmAll`  
`preparedQuantity` is `null` before finalization, set after `POST .../finalize`.

When a session passes `finalizationDeadline` without manual finalization:

- `AutoReject`: cancels affected orders, marks pending/change-pending items as refunded, creates a full-order `RefundRequest` for paid orders using `CHANGE_PROPOSAL / REFUND / ORDER_REFUND_POLICY_CODE`, **auto-approves and credits it to the customer's wallet immediately** (no manager review — same auto-approval rule as `ChangeProposal`-driven refunds, see `refund-api.md`/`change-proposal-refund-flow.md`; `reviewedBy = null`), and sends realtime notifications.
- `AutoConfirmAll`: sets each dish `preparedQuantity` to the total ordered quantity, confirms pending order items, and sends realtime notifications.

`isActive` in session responses is the manager-controlled active flag.
Ordering is still enforced by time rules on cart/order APIs: users can order only when `now >= availableForOrder` and `now <= availableTo`.

**Reading `finalizationDeadline` after finalization.** Use `isFinalized` — never the deadline — to decide whether a session can still be finalized:
- `POST .../finalize` leaves `finalizationDeadline` untouched, so a finalized session can legitimately still show a deadline in the future.
- `POST .../finalize-now` pulls `finalizationDeadline` (and `availableFrom`) back to the moment of the call, so both read as "now" afterwards. See [`finalize-session-now-api.md`](finalize-session-now-api.md).

Either way, ordering has already stopped the instant `isFinalized` becomes `true` — the cart/order APIs reject a finalized session regardless of the deadline or the time window.

---

## `GET /api/sessions`
AllowAnonymous.  
**Query:** `?name=string&isActive=bool&pageNumber=1&pageSize=10`

Paginated items include full dish details and meal template settings:
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "isActive": true,
  "availableFrom": "...",
  "availableTo": "...",
  "availableForOrder": "...",
  "finalizationDeadline": "... | null",
  "autoFinalizePolicy": 0,
  "isFinalized": false,
  "finalizedAtUtc": null,
  "createdAtUtc": "...",
  "updatedAtUtc": null,
  "createdBy": "guid",
  "mealTemplates": [
    {
      "id": "guid",
      "name": "string",
      "settings": [
        {
          "id": "guid",
          "mealTemplateId": "guid",
          "categoryId": "guid",
          "categoryName": "string",
          "minQuantity": 1,
          "maxQuantity": 3,
          "isRequired": true
        }
      ]
    }
  ],
  "dishes": [
    {
      "id": "guid",
      "dishId": "guid",
      "dishName": "string",
      "imgUrl": "string | null",
      "priceAmount": 25.0,
      "priceCurrency": "Point",
      "categoryId": "guid",
      "categoryName": "string",
      "preparedQuantity": null
    }
  ]
}
```

---

## `GET /api/sessions/{id}`
AllowAnonymous.

Same shape as list item. `404` if not found.

---

## `GET /api/sessions/calendar`
AllowAnonymous.

**Query:** `?year=2026`

Returns every day of the given year with the number of sessions on that day.
`days` always contains one entry per calendar day — `365`, or `366` in a leap year — including days with `sessionCount: 0`.

Notes:

- Timezone is `Asia/Ho_Chi_Minh`; a session is counted on each local date its `availableFrom`–`availableTo` window covers.
- A session ending exactly at local midnight counts for the previous day only.
- `totalSessions` counts distinct sessions overlapping the year, not the sum of `sessionCount` (a multi-day session is counted once).
- Soft-deleted sessions are excluded.
- `year` must be between `2000` and `2100`, otherwise `400 InvalidValue`.

```json
{
  "value": {
    "year": 2026,
    "timezone": "Asia/Ho_Chi_Minh",
    "totalDays": 365,
    "totalSessions": 42,
    "days": [
      { "date": "2026-01-01", "sessionCount": 0 },
      { "date": "2026-01-02", "sessionCount": 2 },
      { "date": "2026-01-03", "sessionCount": 1 }
    ]
  },
  "isSuccess": true,
  "message": "Session calendar retrieved successfully."
}
```

---

## `POST /api/sessions`
Authorize.

```json
{
  "name": "string",
  "description": "string",
  "availableFrom": "...",
  "availableTo": "...",
  "availableForOrder": "...",
  "finalizationDeadline": "... | null",
  "autoFinalizePolicy": 0,
  "mealTemplates": [
    {
      "name": "string",
      "settings": [
        {
          "categoryId": "guid",
          "minQuantity": 1,
          "maxQuantity": 3,
          "isRequired": true
        }
      ]
    }
  ],
  "dishes": [
    { "dishId": "guid" }
  ]
}
```

**201:** Returns `Location` header to `GET /api/sessions/{id}`.
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "isActive": true,
    "availableFrom": "...",
    "availableTo": "...",
    "availableForOrder": "...",
    "finalizationDeadline": "... | null",
    "autoFinalizePolicy": 0,
    "createdAtUtc": "...",
    "createdBy": "guid",
    "message": "Session created successfully."
  },
  "isSuccess": true
}
```

**400 validation examples:**
- `AvailableFrom must be before AvailableTo.`
- `AvailableForOrder must be before or equal to AvailableFrom.`
- `AutoFinalizePolicy is invalid.`
- `FinalizationDeadline must be in the future.`
- `Session time overlaps with another session.`

---

## `PUT /api/sessions/{id}`
Authorize. Route `id` must match body `id` (else `400`).

Updates are allowed only before ordering opens for the current session:
`now < current availableForOrder`.

`mealTemplates` are replaced by the submitted list. `dishes` are treated as the final submitted list: existing dishes that remain are kept, missing dishes are removed, and new dishes are added.

```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "isActive": true,
  "availableFrom": "...",
  "availableTo": "...",
  "availableForOrder": "...",
  "finalizationDeadline": "... | null",
  "autoFinalizePolicy": 0,
  "mealTemplates": [
    {
      "name": "string",
      "settings": [
        {
          "categoryId": "guid",
          "minQuantity": 1,
          "maxQuantity": 3,
          "isRequired": true
        }
      ]
    }
  ],
  "dishes": [
    { "dishId": "guid" }
  ]
}
```

**200:**
```json
{ "value": { "id": "guid", "name": "string", "message": "Session updated successfully." }, "isSuccess": true }
```

**400 validation examples:**
- `AvailableFrom must be before AvailableTo.`
- `AvailableForOrder must be before or equal to AvailableFrom.`
- `AutoFinalizePolicy is invalid.`
- `FinalizationDeadline must be in the future.`
- `Session cannot be updated after ordering has opened.`
- `Session time overlaps with another session.`

---

## `DELETE /api/sessions/{id}`
Authorize. Soft-delete **plus automatic refunds** for every order the robot has not started serving.

Deleting a session used to strand the money of anyone who had already ordered from it. It now settles
those orders in the same transaction as the delete:

| Order state | What happens |
|---|---|
| No serving job yet, or job still `Queued` | Cancelled, full-order refund **auto-approved and credited immediately**, serving job cancelled, any held tray released back to the pool, open change proposals closed as `OrderRefundRequested`, customer notified |
| Job `Pushed`, `Assembling`, `OnShelf` or `Failed` | **Left untouched** — the food is already on its way to a tray, so the customer still collects it even though the session is gone |
| Already `Cancelled` / `Expired` / `Completed` | Ignored, nothing to settle |
| Another full-order refund already `Pending`/`Approved` | Skipped, so the order is never credited twice |

The `Queued`-only boundary is the same one the customer-facing full-order refund enforces
(see [`change-proposal-api.md`](change-proposal-api.md)).

**Refund amount** covers every line not already `Refunded`, under the policy configured at
`SESSION / REFUND / DELETE_REFUND_POLICY_CODE`.

This flow has **its own policy**, deliberately not the change-proposal one: the refund report groups by
`PolicyCode`, so sharing a code would merge "manager removed the session" and "kitchen came up short"
into a single unreadable line. Terms are identical (100%, no evidence photo); only the identity differs.

Required settings:

| Group | Scope | Code | Value |
|-------|-------|------|-------|
| `SESSION` | `REFUND` | `DELETE_REFUND_POLICY_CODE` | policy code, e.g. `SESSION_DELETED_NO_IMAGE` |
| `REFUND_POLICY` | `SESSION_DELETED_NO_IMAGE` | `NAME` | e.g. `Refund for a deleted session` |
| `REFUND_POLICY` | `SESSION_DELETED_NO_IMAGE` | `DESCRIPTION` | free text |
| `REFUND_POLICY` | `SESSION_DELETED_NO_IMAGE` | `PERCENT` | `100` |
| `REFUND_POLICY` | `SESSION_DELETED_NO_IMAGE` | `REQUIRES_IMAGE` | `false` |

The policy code is read from the setting, not hardcoded, so an instance can point this flow at a
different policy without a code change. The policy must exist and must not require images, **but only
when there is at least one order to refund**: a session with nothing to settle deletes fine on an
instance where it was never configured.

All-or-nothing: if crediting any wallet fails, nothing is deleted, cancelled or credited, and the call
returns the credit error. Customer notifications are only sent after the transaction commits.

**200:**
```json
{
  "value": {
    "id": "guid",
    "refundedOrderCount": 2,
    "skippedOrderCount": 1,
    "message": "Session deleted successfully (soft delete). 2 order(s) were cancelled and refunded."
  },
  "isSuccess": true
}
```

`skippedOrderCount` counts orders left alone — either the robot had already started them, or another
full-order refund was in flight. With nothing to refund the message stays
`Session deleted successfully (soft delete).` and both counts are `0`.

**400:**

| Error | Message | When |
|---|---|---|
| `InvalidValue` | `Session deletion refund policy is not configured.` | an order needs refunding but `DELETE_REFUND_POLICY_CODE` is missing or blank |
| `InvalidValue` | `Configured session deletion refund policy is not active or invalid.` | the referenced `REFUND_POLICY` scope is incomplete |
| `InvalidValue` | `Configured session deletion refund policy cannot require images.` | the policy has `REQUIRES_IMAGE = true` |

**404:** `Session with id {id} not found.` — also returned for an already soft-deleted session.

---

## `POST /api/sessions/{id}/finalize`
Authorize role: `Manager`. Manager confirms prepared quantities per dish. Under-supplied items trigger change proposals and realtime notifications.

```json
{
  "preparedDishes": [
    {
      "dishId": "guid",
      "preparedQuantity": 10,
      "suggestedDishId": "guid | null"
    }
  ]
}
```

`suggestedDishId` is optional. When provided, it must be active, part of the same session, different from `dishId`, allowed by affected order templates, in the same required category when the missing dish is required, and priced the same as `dishId` (a swap must be money-neutral, so the customer can only accept an equal-priced dish - see [`change-proposal-api.md`](change-proposal-api.md)). The auto-picked suggestion obeys the same price rule, and is left empty when the category has no equal-priced dish with spare quantity.

**200:**
```json
{ "value": { "message": "Session finalized successfully." }, "isSuccess": true }
```
