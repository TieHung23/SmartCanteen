# SmartCanteen Order API

Base URL: `/api/orders`  
Auth: `[Authorize]`  
API Version: `1.0`  
Currency: **Point**

Orders are scoped to the authenticated user. Manager endpoints not yet exposed.

OrderStatus: `0=Pending, 1=ReadyForPickup, 2=Completed, 3=Cancelled, 4=Preparing, 5=Serving, 6=InHoldingArea, 7=Expired, 8=Disposed`

---

## `GET /api/orders`
**Query:** `?sessionId=guid&status=int&pageNumber=1&pageSize=10`

Paginated items (scoped to current user):
```json
{
  "id": "guid",
  "sessionId": "guid",
  "mealTemplateId": "guid | null",
  "transactionId": "guid | null",
  "userId": "guid",
  "status": 0,
  "totalPrice": 0.0,
  "itemCount": 0,
  "createdAtUtc": "..."
}
```
`totalPrice` is computed server-side from line items.

---

## `GET /api/orders/{id}`
**200:**
```json
{
  "value": {
    "id": "guid",
    "sessionId": "guid",
    "mealTemplateId": "guid | null",
    "transactionId": "guid | null",
    "userId": "guid",
    "status": 0,
    "totalPrice": 0.0,
    "items": [
      {
        "dishId": "guid",
        "quantity": 1,
        "unitPrice": 0.0
      }
    ],
    "createdAtUtc": "...",
    "updatedAtUtc": "... | null"
  },
  "isSuccess": true
}
```
`404` if not found or not owned by current user.

---

## `POST /api/orders`
Reads the user's persisted cart, validates against the session/template, deducts wallet, creates order and wallet transaction atomically.

```json
{ "sessionId": "guid", "cartVersion": 1 }
```

**Validation:**
- Cart version must match server (else `409`)
- Session must exist, be active, not expired
- Template rules (required categories, min/max quantities) enforced
- Sufficient dish stock validated
- Wallet balance must cover total price

**201:**
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
  "isSuccess": true
}
```

---

## `PUT /api/orders/{id}`
```json
{ "status": 0 }
```

**200:**
```json
{ "value": { "id": "guid", "status": 0, "message": "string" }, "isSuccess": true }
```

---

## `DELETE /api/orders/{id}`
**200:**
```json
{ "value": { "id": "guid", "message": "string" }, "isSuccess": true }
```
