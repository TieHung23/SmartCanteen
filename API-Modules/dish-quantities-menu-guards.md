# Session Dish Quantities & Menu Edit Guards

Base URL: `/api/sessions/{id}/dish-quantities`, `/api/dishes`, `/api/categories`
API Version: `1.0`

Two related pieces:

1. A **Manager** read endpoint reporting how much of each dish is currently on order for a session.
2. **Guards** that reject dish/category edits while a *current* session still depends on them.

---

## `GET /api/sessions/{id}/dish-quantities`

Auth: `[Authorize(Roles = "Manager")]`

Returns each dish in the session with the number of portions still on order — what the kitchen actually has to produce.

### Request

| Part | Name | Type | Required | Notes |
|------|------|------|----------|-------|
| Route | `id` | `guid` | yes | Session ID. Must be a non-empty GUID. |

No query string, no body.

```http
GET /api/sessions/6f1d2c9e-4b8a-4d21-9f3e-77c0a1b25e84/dish-quantities
Authorization: Bearer <manager-jwt>
```

### 200 Response

```json
{
  "value": {
    "sessionId": "6f1d2c9e-4b8a-4d21-9f3e-77c0a1b25e84",
    "sessionName": "Lunch 2026-08-11",
    "totalOrderedQuantity": 87,
    "dishes": [
      { "dishId": "b21c...", "dishName": "Cơm Tấm Sườn", "orderedQuantity": 42 },
      { "dishId": "9ae4...", "dishName": "Phở Bò",       "orderedQuantity": 45 },
      { "dishId": "3f77...", "dishName": "Chè Đậu Xanh",  "orderedQuantity": 0 }
    ]
  },
  "message": "Session dish quantities retrieved successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": {}
}
```

| Field | Type | Meaning |
|-------|------|---------|
| `sessionId` | `guid` | Echo of the requested session. |
| `sessionName` | `string` | Session display name. |
| `totalOrderedQuantity` | `int` | Sum of `orderedQuantity` across all rows. |
| `dishes[].dishId` | `guid` | Dish ID. |
| `dishes[].dishName` | `string` | Dish name; empty string if the dish row was hard-deleted. |
| `dishes[].orderedQuantity` | `int` | Portions currently on order. `0` for a menu dish nobody ordered. |

**Sort order:** `orderedQuantity` descending, then `dishName` ascending.

### What counts toward `orderedQuantity`

Counted — orders in status `Pending`, `Preparing`, `ReadyForPickup`, `Completed`.

Not counted:
- Orders in status `Cancelled` or `Expired` — no longer served.
- Soft-deleted orders.
- Line items in status `Refunded`.

Line items in `ChangePending`, `Swapped`, and `RefundPending` **are** counted — they are not settled yet, so the portion may still be owed.

### Which dishes appear

Every dish on the session menu (`SessionDishes`), **plus** any dish with a non-zero ordered quantity that is no longer on the menu. The second case shouldn't normally arise now that the guards below exist, but those portions were paid for and must not silently disappear from the manager's view.

### Errors

| Status | `errorCode` | When |
|--------|-------------|------|
| `400` | `InvalidValue` | `id` is `Guid.Empty`. |
| `404` | `SessionNotFound` | No such session, or it is soft-deleted. |
| `500` | `ServerError` | Unexpected failure. |

---

## Menu Edit Guards

### What "current session" means

A session blocks edits while **all** of these hold:

- `IsDeleted == false`
- `IsActive == true`
- `AvailableTo >= now` (UTC)

This covers sessions still open for ordering *and* sessions mid-service. Sessions whose serving window has closed are history and never block anything.

### Guarded endpoints

| Endpoint | Rejected when |
|----------|---------------|
| `PUT /api/dishes/{id}` | The dish is on the menu of a current session. |
| `DELETE /api/dishes/{id}` | Same. |
| `PUT /api/categories/{id}` | The category is used by a current session. |
| `DELETE /api/categories/{id}` | Any **active** dish is still bound to the category, **or** the category is used by a current session. |

A category counts as *used by a current session* through either path:
- One of its dishes is on that session's menu, or
- One of that session's meal-template settings references the category.

For `DELETE /api/categories/{id}` the active-dish check runs **first**. "Active" means `IsActive && !IsDeleted`; already-deactivated or soft-deleted dishes do not block the delete. To delete a category, deactivate or reassign its dishes first.

### 409 Response

All guard rejections return HTTP `409` with `errorCode: "ResourceBusy"`. The `message` says which rule fired; `reason` names the offending sessions or dishes.

```json
{
  "value": null,
  "message": "Cannot delete a dish that belongs to a current session.",
  "reason": "Currently used by: Lunch 2026-08-11, Dinner 2026-08-11.",
  "isSuccess": false,
  "isFailure": true,
  "error": {},
  "errorCode": "ResourceBusy"
}
```

Messages by case:

| Case | `message` | `reason` |
|------|-----------|----------|
| Dish update blocked | `Cannot update a dish that belongs to a current session.` | `Currently used by: <session names>.` |
| Dish delete blocked | `Cannot delete a dish that belongs to a current session.` | `Currently used by: <session names>.` |
| Category update blocked | `Cannot update a category that is used by a current session.` | `Currently used by: <session names>.` |
| Category delete — active dishes | `Cannot delete a category that still has active dishes.` | `Active dishes: <dish names>.` |
| Category delete — current session | `Cannot delete a category that is used by a current session.` | `Currently used by: <session names>.` |

---

## Status Code Change

The four guarded endpoints now map the error's own HTTP status instead of a fixed one. Previously `DELETE /api/dishes/{id}` returned `404` for *every* failure and the other three returned `400`.

| Endpoint | Before | Now |
|----------|--------|-----|
| `PUT /api/dishes/{id}` | `400` for all failures | `404` not found · `409` guard · `400` validation |
| `DELETE /api/dishes/{id}` | `404` for all failures | `404` not found · `409` guard |
| `PUT /api/categories/{id}` | `400` for all failures | `404` not found · `409` guard |
| `DELETE /api/categories/{id}` | `400` for all failures | `404` not found · `409` guard |

Clients keying off HTTP status rather than the `errorCode` body field need to be checked against this.

---

## Envelope Note

The `error` property always serializes as `{}` — every member of the `Error` type is `[JsonIgnore]`. **Read the machine-readable code from `errorCode`, not from `error`.** `reason` is omitted entirely when not set. This differs from the envelope sketch in [`README.md`](README.md), which shows `error` as a string code.
