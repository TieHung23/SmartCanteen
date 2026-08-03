# SmartCanteen Order API

Base URL: `/api/orders`  
Auth: `[Authorize]`  
API Version: `1.0`  
Currency: **Point**

User endpoints are scoped to the authenticated user. Manager endpoints are scoped by manager role.
Customer orders cannot be modified after payment. Operational status changes are handled by Manager/Staff endpoints.

OrderStatus: `0=Pending, 1=ReadyForPickup, 2=Completed, 3=Cancelled, 4=Preparing, 7=Expired`
(Values `5=Serving`, `6=InHoldingArea`, `8=Disposed` were removed — do not use them.)

---

## `GET /api/orders`
**Query:** `?sessionId=guid&status=int&createdFrom=datetime&createdTo=datetime&sessionDateFrom=datetime&sessionDateTo=datetime&pageNumber=1&pageSize=10`

| Param | Type | Description |
|-------|------|--------------|
| `sessionId` | `Guid?` | Exact match on session |
| `status` | `int?` | `OrderStatus` value, use as combobox filter |
| `createdFrom` / `createdTo` | `DateTimeOffset?` | Filters on `Order.CreatedAtUtc` (order creation time) |
| `sessionDateFrom` / `sessionDateTo` | `DateTimeOffset?` | Filters on `Session.AvailableFrom` (session serving start time) — **not** an overlap check against `AvailableTo`. Sessions do not span midnight in this system, so a single-day filter is safe to use directly |

Paginated items (scoped to current user):
```json
{
  "id": "guid",
  "sessionId": "guid",
  "sessionName": "string",
  "mealTemplateId": "guid | null",
  "transactionId": "guid | null",
  "userId": "guid",
  "status": 0,
  "totalPrice": 0.0,
  "itemCount": 0,
  "createdAtUtc": "..."
}
```
`totalPrice` is computed server-side from line items. `sessionName` is new — added for list-screen display without a second call to `GET /api/Sessions/{id}`.

---

## `GET /api/manager/orders/session/{sessionId}`
Auth: `Manager` or `Staff`

**Query:** `?status=int&pageNumber=1&pageSize=10`

Returns all orders in the session, not scoped to the current user:
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

---

## `GET /api/manager/orders/{id}`
Auth: `Manager` or `Staff`

Returns one customer order detail for operations staff, including `items` and `statusHistories`.

**200:** same response shape as `GET /api/orders/{id}`.

---

## `PUT /api/manager/orders/{id}/status`
Auth: `Manager` or `Staff`

Updates an order status and writes one row to `OrderStatusHistories` with `reasonCode = "ManualUpdate"`.

```json
{ "status": 1 }
```

**200:**
```json
{ "value": { "id": "guid", "status": 1, "message": "string" }, "isSuccess": true }
```

---

## `GET /api/orders/{id}`
Auth: authenticated customer. The order must belong to the current user.

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
        "dishName": "string",
        "imgUrl": "string | null",
        "quantity": 1,
        "unitPrice": 0.0
      }
    ],
    "statusHistories": [
      {
        "id": "guid",
        "fromStatus": 0,
        "toStatus": 4,
        "reasonCode": "RobotPickStarted | AssignedToPickupSlot | Collected | ManualUpdate | string | null",
        "note": "string | null",
        "createdAtUtc": "...",
        "createdBy": "guid"
      }
    ],
    "createdAtUtc": "...",
    "updatedAtUtc": "... | null"
  },
  "isSuccess": true
}
```
`403` if the order does not belong to the current user.  
`404` if not found.

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
    "message": "Order created successfully and wallet debited.",
    "userRemainingBalance": 0.0,
    "cartVersion": 2
  },
  "isSuccess": true
}
```
`message` doubles as the human-readable transaction description for the UI.

---

## `DELETE /api/orders/{id}`
Auth: authenticated customer. The order must belong to the current user.

Only unpaid orders can be deleted. Paid orders return `409 ResourceBusy` and must not be edited after checkout/payment.

**200:**
```json
{ "value": { "id": "guid", "message": "string" }, "isSuccess": true }
```
