# SmartCanteen Session API

Base URL: `/api/sessions`  
API Version: `1.0`

Auth: `GET` endpoints are `[AllowAnonymous]`. All other endpoints require `[Authorize]`.

AutoFinalizePolicy: `0=AutoReject, 1=AutoConfirmAll`  
`preparedQuantity` is `null` before finalization, set after `POST .../finalize`.

`isActive` in session responses means the session is currently open for ordering:
`manual IsActive = true`, not deleted, `now >= availableForOrder`, and `now <= availableTo`.
Future sessions return `isActive=false` until `availableForOrder`.

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
Authorize. Manager confirms prepared quantities per dish. Under-supplied items trigger change proposals.

```json
{
  "preparedDishes": [
    { "dishId": "guid", "preparedQuantity": 10 }
  ]
}
```

**200:**
```json
{ "value": { "message": "Session finalized successfully." }, "isSuccess": true }
```
