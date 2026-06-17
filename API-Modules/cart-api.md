# SmartCanteen Cart API

Base URL: `/api/cart`  
Authentication: required

Each authenticated user has at most one persisted cart. `UserId` is resolved from
the access token and is never accepted from the request body.

## `GET /api/cart`

Returns the current user's cart. A user without a cart receives an empty cart with
version `0`. Expired, inactive, or deleted meals are removed from the persisted cart
before the response is returned.

```json
{
  "value": {
    "id": null,
    "data": {
      "meals": []
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
    "meals": [
      {
        "mealId": "guid",
        "mealTemplateId": "guid",
        "items": [
          {
            "dishId": "guid",
            "quantity": 2
          }
        ]
      }
    ]
  },
  "expectedVersion": 0
}
```

Validation when saving cart:

- `mealId`, `mealTemplateId`, and every `dishId` are required.
- A cart can contain multiple meals, but each `mealId` can appear only once.
- The template must belong to the selected meal.
- The meal and dishes must exist, be active, and not be deleted.
- The ordering deadline must not have passed.
- Every dish must belong to the selected meal.
- Selected dish categories must be allowed by the template.
- Selected category quantities cannot exceed template maximums.
- Requested quantity cannot exceed the dish stock configured for the meal.
- The cart must contain at least one dish.
- Quantity must be greater than zero and cannot exceed current stock.
- Duplicate dishes are rejected.

Saving a cart allows partial selections, so required categories and template minimums
are enforced during checkout instead of during `PUT /api/cart`.

The persisted `Carts.DataJson` value is stored as the meal list itself:
`[{"mealId":"guid","mealTemplateId":"guid","items":[...]}]`.

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
  "mealId": "guid",
  "cartVersion": 1
}
```

The server validates the selected meal in the cart again with complete template rules,
including required categories and category minimum quantities. It then atomically reserves
dish stock and uses current database prices. Stock reservation, wallet debit, wallet
transaction creation, order creation, and removing that meal from the cart occur in one
database transaction. If any step fails, all changes are rolled back.
