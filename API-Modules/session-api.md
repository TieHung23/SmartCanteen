# SmartCanteen Session API

> Generated from `API-Document.md`. Run `.\tools\generate-api-modules.ps1` after updating the main document.

# SmartCanteen API Documentation

Base URL: `/api`  
API Version: `1.0`  
All timestamps: `DateTimeOffset` (ISO 8601)  
Currency: **Point** (no currency field exposed)

Module documentation: [`API-Modules/README.md`](API-Modules/README.md)

Every response is wrapped in a standard envelope:

```json
{
  "value": {},
  "isSuccess": true,
  "isFailure": false,
  "message": "string",
  "error": null
}
```

On failure `value` is null, `isSuccess` false, `error` is a string code.

Pagination query param defaults: `pageNumber=1`, `pageSize=10` (max 100).  
Paginated response shape:

```json
{
  "value": {
    "items": [],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 42,
    "totalPages": 5,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "isSuccess": true,
  "message": "string"
}
```

---

## Sessions

### `GET /api/sessions`

**Auth:** AllowAnonymous  
**Query:** `?name=string&isActive=bool&pageNumber=1&pageSize=10`

**Paginated response items:**

```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "isActive": true,
  "availableFrom": "2024-01-01T00:00:00Z",
  "availableTo": "2024-01-01T00:00:00Z",
  "availableForOrder": "2024-01-01T00:00:00Z",
  "finalizationDeadline": "2024-01-01T00:00:00Z",
  "autoFinalizePolicy": 0,
  "isFinalized": false,
  "finalizedAtUtc": null,
  "createdAtUtc": "2024-01-01T00:00:00Z",
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
      "imgUrl": "string",
      "priceAmount": 25.0,
      "priceCurrency": "Point",
      "categoryId": "guid",
      "preparedQuantity": null
    }
  ]
}
```

### `GET /api/sessions/{id}`

**Auth:** AllowAnonymous

**Response:**

```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "isActive": true,
    "availableFrom": "2024-01-01T00:00:00Z",
    "availableTo": "2024-01-01T00:00:00Z",
    "availableForOrder": "2024-01-01T00:00:00Z",
    "finalizationDeadline": "2024-01-01T00:00:00Z",
    "autoFinalizePolicy": 0,
    "isFinalized": false,
    "finalizedAtUtc": null,
    "createdAtUtc": "2024-01-01T00:00:00Z",
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
            "minQuantity": 1,
            "maxQuantity": 3,
            "isRequired": true
          }
        ]
      }
    ],
    "dishes": [{ "id": "guid", "dishId": "guid", "preparedQuantity": null }]
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/sessions`

**Auth:** Authorize

**Request body:**

```json
{
  "name": "string",
  "description": "string",
  "availableFrom": "2024-01-01T00:00:00Z",
  "availableTo": "2024-01-01T00:00:00Z",
  "availableForOrder": "2024-01-01T00:00:00Z",
  "finalizationDeadline": "2024-01-01T00:00:00Z",
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
  "dishes": [{ "dishId": "guid" }]
}
```

**201 Response:**

```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "isActive": true,
    "availableFrom": "2024-01-01T00:00:00Z",
    "availableTo": "2024-01-01T00:00:00Z",
    "availableForOrder": "2024-01-01T00:00:00Z",
    "finalizationDeadline": "2024-01-01T00:00:00Z",
    "autoFinalizePolicy": 0,
    "createdAtUtc": "2024-01-01T00:00:00Z",
    "createdBy": "guid",
    "message": "Session created successfully."
  },
  "isSuccess": true,
  "message": "string"
}
```

### `PUT /api/sessions/{id}`

**Auth:** Authorize

**Request body:**

```json
{
  "name": "string",
  "description": "string",
  "isActive": true,
  "availableFrom": "2024-01-01T00:00:00Z",
  "availableTo": "2024-01-01T00:00:00Z",
  "availableForOrder": "2024-01-01T00:00:00Z",
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
  "dishes": [{ "dishId": "guid" }]
}
```

**Response:**

```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "message": "string"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `DELETE /api/sessions/{id}`

**Auth:** Authorize

**Response:**

```json
{
  "value": {
    "id": "guid",
    "message": "string"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/sessions/{id}/finalize`

**Auth:** Authorize (Manager)  
**Description:** Manager confirms prepared quantities for each dish in the session. Items with sufficient stock are confirmed; items without are marked for change proposals.

**Request body:**

```json
{
  "preparedDishes": [{ "dishId": "guid", "preparedQuantity": 10 }]
}
```

**Response:**

```json
{
  "value": {
    "message": "Session finalized successfully."
  },
  "isSuccess": true,
  "message": "string"
}
```

---
