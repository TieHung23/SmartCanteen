# Order API Documentation

**Base URL:** `/api/orders`  
**API Version:** `1.0`  
**Auth:** All endpoints require `[Authorize]` — a valid JWT bearer token.

---

## Common Response Envelope

Every endpoint returns a `Result<T>` wrapper:

```json
{
  "message": "string | null",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": { /* T payload */ }
}
```

On failure:

```json
{
  "message": "string | null",
  "isSuccess": false,
  "isFailure": true,
  "error": null,
  "value": null
}
```

> `Error` fields (`Code`, `Message`, `HttpStatusCode`) are decorated with `[JsonIgnore]` and **not serialized** in the response body. The HTTP status code is determined by the controller action (`400 BadRequest`, `404 NotFound`, `500` is caught and wrapped by handler).

---

## Order Status Enum

| Value | Name             |
|-------|------------------|
| 0     | Pending          |
| 1     | ReadyForPickup   |
| 2     | Completed        |
| 3     | Cancelled        |

---

## `GET /api/orders`
**Auth:** Required  
**Summary:** Get paginated orders for the current user (or a specific user if admin).

### Request — Query Parameters

| Parameter    | Type    | Required | Default | Description                                      |
|-------------|---------|----------|---------|--------------------------------------------------|
| PageNumber  | int     | No       | 1       | Page index (clamped to >= 1)                     |
| PageSize    | int     | No       | 10      | Items per page (clamped 1–100)                   |
| UserId      | guid    | No       | null    | Filter by specific user (defaults to current user) |
| Status      | int     | No       | null    | Filter by order status (0–3)                     |

### Response 200

```json
{
  "message": "Orders retrieved successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": {
    "items": [
      {
        "id": "guid",
        "mealId": "guid",
        "transactionId": "guid | null",
        "userId": "guid",
        "status": 0,
        "totalPrice": 0.0,
        "itemCount": 0,
        "createdAtUtc": "2025-01-01T00:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 42,
    "totalPages": 5,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

### Validation Rules (Handler-Enforced)

None — query params have safe defaults via `PaginationParams` property setters.

### Handler Logic (`GetAllOrdersQueryHandler`)

1. Build queryable from `orderRepository.GetQueryable()`
2. If `UserId` is not provided, filter to `currentUserService.UserId`
3. If `Status` has value, filter `x.Status == (OrderStatus)request.Status`
4. Count total matching records
5. Apply `OrderByDescending(x.CreatedAtUtc)`, skip + take pagination
6. Map to `GetAllOrdersResponse` list (computing `TotalPrice` from items)
7. Wrap in `PaginatedList<T>` and return `Result.Success`

### Status Codes

| Code | Description                          |
|------|--------------------------------------|
| 200  | Success — orders returned            |
| 400  | Failure — server error               |

---

## `GET /api/orders/{id:guid}`
**Auth:** Required  
**Summary:** Get a single order with all line items.

### Request — Route Parameter

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id        | guid | Yes      | Order ID    |

### Response 200

```json
{
  "message": "Order retrieved successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": {
    "id": "guid",
    "mealId": "guid",
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
    "createdAtUtc": "2025-01-01T00:00:00Z",
    "updatedAtUtc": null
  }
}
```

### Response 404

```json
{
  "message": "Order with id {id} not found.",
  "isSuccess": false,
  "isFailure": true,
  "error": null,
  "value": null
}
```

### Handler Logic (`GetOrderByIdQueryHandler`)

1. Fetch order by `request.Id` from `orderRepository.GetByIdAsync()`
2. If `null`, return `Result.Failure` with `Error.NullValue`
3. Compute `TotalPrice` by summing `OrderItems` (UnitPrice × Quantity)
4. Map order + items to `GetOrderByIdResponse`
5. Return `Result.Success`

### Status Codes

| Code | Description                          |
|------|--------------------------------------|
| 200  | Success — order returned             |
| 404  | Order not found                      |
| 500  | Server error                         |

---

## `POST /api/orders`
**Auth:** Required  
**Summary:** Create an order, deduct total from user wallet, record a wallet transaction.

### Request — JSON Body

```json
{
  "mealId": "guid",
  "items": [
    {
      "dishId": "guid",
      "quantity": 1
    }
  ]
}
```

### Response 201

```json
{
  "message": "Order created successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": {
    "id": "guid",
    "transactionId": "guid",
    "totalPrice": 0.0,
    "message": "Order created successfully and wallet debited.",
    "userRemainingBalance": 0.0
  }
}
```

### Validation Rules (Handler-Enforced)

| Field                | Rule                                              |
|----------------------|---------------------------------------------------|
| `items`              | Must contain at least one item                    |
| `items[].quantity`   | Must be greater than zero (each item)             |
| `mealId`             | Must reference an existing meal (via domain usage) |
| `items[].dishId`     | Must reference an existing, non-deleted, active dish |
| wallet balance       | Must be >= `totalPrice`                           |

### Handler Logic (`CreateOrderCommandHandler`)

1. Validate `Items` is not empty and all quantities > 0
2. Resolve current user from `currentUserService.UserId`
3. Fetch user; return failure if not found
4. Fetch all dishes by distinct `DishId`s
5. If dish count mismatch, return "One or more dishes not found"
6. If any dish is deleted or inactive, return "not available"
7. Compute `totalPrice` = Σ(dish.Price × quantity)
8. If user balance < totalPrice, return insufficient balance error
9. Begin EF Core transaction
10. Create `Order` aggregate via `Order.Create(mealId, currentUserId)`
11. Add each dish item to the order via `order.AddDish()`
12. Deduct totalPrice from `user.Balance`
13. Create `WalletTransaction` (type `OrderPayment`, negative amount)
14. Persist transaction, update user, persist order
15. Attach transaction reference to order via `order.AttachTransaction()`
16. SaveChanges + Commit
17. Return `CreateOrderResponse` with new order ID, transaction ID, total, remaining balance
18. On exception: rollback, log error, return `Result.Failure`

### Status Codes

| Code | Description                          |
|------|--------------------------------------|
| 201  | Created — order + wallet deduction   |
| 400  | Validation failure (items, dishes, balance) |
| 500  | Server error                         |

---

## `PUT /api/orders/{id:guid}`
**Auth:** Required  
**Summary:** Update an order's status.

### Request — Route + JSON Body

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id        | guid | Yes      | Order ID (route) |

```json
{
  "id": "guid",
  "status": 1
}
```

> The route `id` overrides the `id` in the body if they differ (see controller line 94–97).

### Response 200

```json
{
  "message": "Order updated successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": {
    "id": "guid",
    "status": 1,
    "message": "Order status updated to ReadyForPickup."
  }
}
```

### Validation Rules (Handler-Enforced)

| Field    | Rule                                              |
|----------|---------------------------------------------------|
| `status` | Must be a valid `OrderStatus` enum value (0–3)    |
| `id`     | Must reference an existing order                  |

### Handler Logic (`UpdateOrderCommandHandler`)

1. Fetch order by `request.Id`
2. If not found, return `Result.Failure` with `Error.NullValue`
3. Validate `request.Status` is defined in `OrderStatus` enum
4. Begin transaction
5. Call `order.UpdateStatus(newStatus, currentUserId)` on the aggregate
6. Mark order as updated in repository
7. SaveChanges + Commit
8. Return `UpdateOrderResponse` with new status and message
9. On exception: rollback, log error, return `Result.Failure`

### Status Codes

| Code | Description                          |
|------|--------------------------------------|
| 200  | Success — status updated             |
| 400  | Invalid status value                 |
| 404  | Order not found                      |
| 500  | Server error                         |

---

## `DELETE /api/orders/{id:guid}`
**Auth:** Required  
**Summary:** Soft-delete an order.

### Request — Route Parameter

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id        | guid | Yes      | Order ID    |

### Response 200

```json
{
  "message": "Order deleted successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": {
    "id": "guid",
    "message": "Order deleted successfully (soft delete)."
  }
}
```

### Validation Rules (Handler-Enforced)

| Field | Rule                              |
|-------|-----------------------------------|
| `id`  | Must reference an existing order  |

### Handler Logic (`DeleteOrderCommandHandler`)

1. Fetch order by `request.Id`
2. If not found, return `Result.Failure` with `Error.NullValue`
3. Begin transaction
4. Call `order.SoftDelete()` on the aggregate
5. Mark order as updated in repository
6. SaveChanges + Commit
7. Return `DeleteOrderResponse` with confirmation message
8. On exception: rollback, log error, return `Result.Failure`

### Status Codes

| Code | Description                          |
|------|--------------------------------------|
| 200  | Success — soft-deleted               |
| 404  | Order not found                      |
| 500  | Server error                         |
