# SmartCanteen Order API

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
  "value": { },
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
    "items": [ ],
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

## Orders

Each Order references a `WalletTransaction` via `transactionId` (see WalletTransaction section). When an order is created, a WalletTransaction (type `OrderPayment`) is automatically generated to record the wallet debit.

### `GET /api/orders`
**Auth:** Authorize  
**Query:** `?userId=guid&status=int&pageNumber=1&pageSize=10`  
Status: `0=Pending, 1=ReadyForPickup, 2=Completed, 3=Cancelled`

**Paginated response items:**
```json
{
  "id": "guid",
  "mealId": "guid",
  "mealTemplateId": "guid | null",
  "transactionId": "guid | null",
  "userId": "guid",
  "status": 0,
  "totalPrice": 0.0,
  "itemCount": 0,
  "createdAtUtc": "2024-01-01T00:00:00Z"
}
```

### `GET /api/orders/{id}`
**Auth:** Authorize  

**Response:**
```json
{
  "value": {
    "id": "guid",
  "mealId": "guid",
  "mealTemplateId": "guid | null",
  "transactionId": "guid | null",
  "userId": "guid",
    "status": 0,
    "totalPrice": 0.0,
    "items": [
      { "dishId": "guid", "quantity": 1, "unitPrice": 0.0 }
    ],
    "createdAtUtc": "2024-01-01T00:00:00Z",
    "updatedAtUtc": "2024-01-01T00:00:00Z | null"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/orders`
**Auth:** Authorize  

**Request body:**
```json
{
  "cartVersion": 1
}
```

The server reads the authenticated user's cart, validates the current meal, dishes and
stock, uses current database prices, atomically reserves stock, debits the wallet,
creates the order and wallet transaction, then clears the cart in one database
transaction. A stale cart version or insufficient stock returns `409`.

**201 Response:**
```json
{
  "value": {
    "id": "guid",
  "transactionId": "guid",
  "totalPrice": 0.0,
  "message": "string",
  "userRemainingBalance": 0.0,
  "cartVersion": 2
  },
  "isSuccess": true,
  "message": "string"
}
```

### `PUT /api/orders/{id}`
**Auth:** Authorize  

**Request body:**
```json
{
  "status": 0
}
```

**Response:**
```json
{
  "value": {
    "id": "guid",
    "status": 0,
    "message": "string"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `DELETE /api/orders/{id}`
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

---

