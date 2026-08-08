# Finalize Session (Start Serving Now) — Demo API

Base URL: `/api/sessions`
API Version: `1.0`

This is a **demo/ops-only variant** of the regular finalize endpoint (`POST /api/sessions/{id}/finalize`, documented in [`session-api.md`](session-api.md)). Use it when you need robots to start serving **immediately** after finalizing, instead of waiting for the session's originally scheduled `availableFrom` time.

---

## `POST /api/sessions/{id}/finalize-now`

Authorize role: **`Manager`**

Does everything the regular `finalize` endpoint does (confirms prepared quantities, creates change proposals for under-supplied items, sends realtime notifications) **plus**: pulls the session's `availableFrom` forward to "now" so already-queued serving jobs immediately become eligible for robot pickup (robots only pull jobs whose session has `availableFrom <= now <= availableTo`).

If `availableFrom` is already in the past (session already open), nothing changes there — this call is then equivalent to the regular finalize.

### Request

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

Same rules as the regular finalize: `suggestedDishId` is optional; when provided it must be active, part of the same session, different from `dishId`, allowed by affected order templates, and in the same required category when the missing dish is required.

### 200 — Success

```json
{
  "value": { "message": "Session finalized and serving started immediately." },
  "isSuccess": true
}
```

### 400 — Failure

All failures return HTTP `400` with:
```json
{
  "isSuccess": false,
  "errorCode": "InvalidValue",
  "message": "..."
}
```

| `errorCode` | `message` | When |
|---|---|---|
| `NullValue` | `Session not found.` | `id` doesn't match any non-deleted session |
| `InvalidValue` | `Session is already finalized.` | session was already finalized (by either endpoint) |
| `InvalidValue` | `Prepared dishes cannot contain duplicate dish IDs.` | same `dishId` sent twice in `preparedDishes` |
| `InvalidValue` | `Finalization deadline has passed.` | session has a `finalizationDeadline` and it already passed |
| `InvalidValue` | `Session's available window has already ended; cannot start serving now.` | **finalize-now only** — session's `availableTo` is already in the past, so starting service now is meaningless |
| `InvalidValue` | `This session's serving window would overlap with another session; sessions are not allowed to overlap.` | **finalize-now only** — pulling `availableFrom` to now would widen this session's window to `[now, availableTo]`, and that widened window overlaps another (different, non-deleted) session's full `[availableFrom, availableTo]` window. This also catches a session that hasn't started yet but would start inside the widened window — not just one that's active right this second |
| `NullValue` | `Dish {dishId} is not part of this session.` | a `dishId` in `preparedDishes` isn't one of the session's dishes |
| `InvalidValue` | `Suggested dish for {dishId} must be different from the current dish.` | `suggestedDishId == dishId` |
| `InvalidValue` | `Suggested dish {suggestedDishId} is not part of this session.` | suggested dish isn't offered in this session |
| `NullValue` | `Suggested dish {suggestedDishId} was not found or is inactive.` | suggested dish doesn't exist, is deleted, or `isActive: false` |
| `NullValue` | `Current dish {dishId} was not found.` | the dish being replaced doesn't exist or is deleted |
| `InvalidValue` | `Suggested dish {suggestedDishId} is not allowed by one or more affected order templates.` | suggested dish's category isn't allowed by the meal template of an affected order |
| `InvalidValue` | `Suggested dish {suggestedDishId} must be in the same required category as dish {dishId}.` | dish is a required item and the suggestion changes its required category |
| `InvalidValue` | `Suggested dish {suggestedDishId} must be in the same category as optional dish {dishId}.` | dish is optional and the suggestion changes its category |

---

## How this differs from `POST /api/sessions/{id}/finalize`

| | `finalize` (regular button) | `finalize-now` (demo button) |
|---|---|---|
| Confirms prepared quantities / creates change proposals | ✅ | ✅ |
| Sends realtime notifications | ✅ | ✅ |
| Changes `availableFrom` | ❌ never | ✅ pulls it to "now", only if it was still in the future |
| Fails if the widened window would overlap another session | ❌ no such check | ✅ blocked with `This session's serving window would overlap with another session; sessions are not allowed to overlap.` (checks full session windows, not just "who's active right now") |
| Robots can pick up queued jobs | only once the originally scheduled `availableFrom` arrives | immediately after the call succeeds |

### FE suggestion
Show `finalize-now` as a distinct "Finalize & Start Serving Now" action (e.g. in a demo/admin panel), separate from the normal "Finalize" button — they hit different endpoints and `finalize-now` can fail for a reason (overlaps another session's window) that `finalize` never does, so surface that error message distinctly to the manager.
