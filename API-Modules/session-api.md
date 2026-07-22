# SmartCanteen Session API

Base URL: `/api/sessions`  
API Version: `1.0`

Auth: `GET` endpoints are `[AllowAnonymous]`. All other endpoints require `[Authorize]`.

AutoFinalizePolicy: `0=AutoReject, 1=AutoConfirmAll`  
`preparedQuantity` is `null` before finalization, set after `POST .../finalize`.

When a session passes `finalizationDeadline` without manual finalization:

- `AutoReject`: cancels affected orders, marks pending/change-pending items as refunded, creates a full-order `RefundRequest` for paid orders using `CHANGE_PROPOSAL / REFUND / ORDER_REFUND_POLICY_CODE`, and sends realtime notifications.
- `AutoConfirmAll`: sets each dish `preparedQuantity` to the total ordered quantity, confirms pending order items, and sends realtime notifications.

`isActive` in session responses is the manager-controlled active flag.
Ordering is still enforced by time rules on cart/order APIs: users can order only when `now >= availableForOrder` and `now <= availableTo`.

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
Authorize. Soft-delete.

**200:**
```json
{ "value": { "id": "guid", "message": "Session deleted successfully (soft delete)." }, "isSuccess": true }
```

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

`suggestedDishId` is optional. When provided, it must be active, part of the same session, different from `dishId`, allowed by affected order templates, and in the same required category when the missing dish is required.

**200:**
```json
{ "value": { "message": "Session finalized successfully." }, "isSuccess": true }
```
