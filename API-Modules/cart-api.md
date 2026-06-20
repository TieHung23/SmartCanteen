# SmartCanteen Cart API

Base URL: `/api/cart`  
Auth: `[Authorize]`  
API Version: `1.0`  
Each user has at most one cart. `UserId` is resolved from the access token.  
Optimistic concurrency via `expectedVersion` — returns `409` on stale version.

---

## `GET /api/cart`
Expired/inactive/deleted sessions are removed before response.

```json
{
  "value": {
    "id": "guid | null",
    "data": { "sessions": [ { "sessionId": "guid", "mealTemplateId": "guid", "items": [ { "dishId": "guid", "quantity": 2 } ] } ] },
    "version": 0,
    "updatedAtUtc": null
  },
  "isSuccess": true
}
```
A new user receives version `0` with empty sessions.

## `PUT /api/cart`
Send `expectedVersion: 0` for a new cart.

```json
{
  "data": {
    "sessions": [
      {
        "sessionId": "guid",
        "mealTemplateId": "guid",
        "items": [ { "dishId": "guid", "quantity": 2 } ]
      }
    ]
  },
  "expectedVersion": 0
}
```

**Validation on save:**
- `sessionId`, `mealTemplateId`, each `dishId` required
- Each `sessionId` once only
- Template belongs to session; session/dish exist, active, not deleted
- Ordering deadline not passed
- Dish belongs to session; category allowed by template; quantity within template max
- Quantity > 0, no duplicate dishes, at least one dish

Template minimums and required categories are enforced at checkout (`POST /api/orders`).

**Response:**
```json
{ "value": { "version": 1, "updatedAtUtc": "..." }, "isSuccess": true }
```

## `DELETE /api/cart?expectedVersion={version}`
Clears cart and increments version. `409` on stale version.
