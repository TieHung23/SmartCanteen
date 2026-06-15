# SmartCanteen Cart API

Base URL: `/api/cart`  
Authentication: required

Each authenticated user has at most one persisted cart. `UserId` is resolved from
the access token and is never accepted from the request body.

## `GET /api/cart`

Returns the current user's cart. A user without a cart receives an empty cart with
version `0`.

```json
{
  "value": {
    "id": null,
    "data": {
      "mealId": "00000000-0000-0000-0000-000000000000",
      "items": []
    },
    "version": 0,
    "updatedAtUtc": null
  },
  "isSuccess": true
}
```

## `PUT /api/cart`

Creates or replaces the complete cart JSON. Use `expectedVersion: 0` when creating
the first cart. Later requests must use the latest version returned by the API.

```json
{
  "data": {
    "mealId": "guid",
    "items": [
      {
        "dishId": "guid",
        "quantity": 2
      }
    ]
  },
  "expectedVersion": 0
}
```

Validation:

- `mealId` and every `dishId` are required.
- The meal and dishes must exist, be active, and not be deleted.
- The ordering deadline must not have passed.
- Every dish must belong to the selected meal.
- Requested quantity cannot exceed the dish stock configured for the meal.
- The cart must contain at least one dish.
- Quantity must be greater than zero and cannot exceed current stock.
- Duplicate dishes are rejected.

A stale version returns HTTP `409` with error code `CartVersionConflict`.

## `DELETE /api/cart?expectedVersion={version}`

Clears the cart and increments its version. A stale version returns HTTP `409`.

## Checkout

Checkout uses the persisted cart:

```http
POST /api/orders
```

```json
{
  "cartVersion": 1
}
```

The server validates the cart again, atomically reserves dish stock, and uses current
database prices. Stock reservation, wallet debit, wallet transaction creation, order
creation, and cart clearing occur in one database transaction. If any step fails, all
changes are rolled back.
