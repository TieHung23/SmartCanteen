# Refund API

Base URL: `/api/refunds` (user) / `/api/manager/refunds` (manager)

---

## `POST /api/refunds`
**Auth:** Authenticated user (`[Authorize]`)

**Content-Type:** `multipart/form-data`

**Request:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `orderId` | `Guid` (form) | Yes | The order to refund |
| `policyCode` | `string` (form) | Yes | Refund policy scope code (e.g. `"quality"`, `"late_delivery"`) |
| `description` | `string` (form) | Yes | Reason for the refund |
| `images` | `List<IFormFile>` (form, optional) | Conditional | Image evidence; required if the policy mandates it (max 5 files) |

**Validation Rules:**
- `OrderId`: must not be empty
- `PolicyCode`: must not be empty, max 255 characters
- `Description`: must not be empty, max 1000 characters
- `Images`: at most 5 files; each file must be an image (MIME type starts with `image/`); validated by `IFileValidator` (file name, size, MIME type)

**Response 201 Created:**
```json
{
  "isSuccess": true,
  "message": "Refund request submitted successfully.",
  "value": {
    "id": "guid",
    "orderId": "guid",
    "policyCode": "string",
    "policyName": "string",
    "refundPercent": 25.0,
    "orderAmount": 100000.0,
    "refundAmount": 25000.0,
    "status": "Pending",
    "imageUrls": ["https://cdn.example.com/refunds/abc.jpg"],
    "createdAtUtc": "2026-06-10T12:00:00Z"
  }
}
```

**Error Responses:**
| Status | Condition |
|--------|-----------|
| `400` | User not authenticated (Forbidden) |
| `400` | Order not found or does not belong to current user |
| `400` | Order already has a pending/approved refund request |
| `400` | Refund policy is not active or invalid |
| `400` | Policy requires image but none provided |
| `400` | Image validation failure (format, size) |
| `400` | Order amount is zero or negative |
| `500` | Image upload failure or unexpected server error |

**Handler Logic:**
1. Resolve `userId` from `ICurrentUserService` — fail if empty.
2. Fetch the order by `orderId` scoped to `userId` and not deleted — fail if not found.
3. Check no active (Pending / Approved) refund request already exists for this order.
4. Load settings from `Setting` table filtered by group `REFUND_POLICY` and matching `policyCode` (case-insensitive). Parse via `RefundPolicyDefinition.TryCreate()` — fail if policy is invalid or inactive.
5. If the resolved policy `RequiresImage` is `true` and no images are provided — fail.
6. Validate each image: MIME type must start with `image/`; run through `IFileValidator`.
7. Compute `orderAmount` as `SUM(orderItem.UnitPrice.Amount * orderItem.Quantity)`.
8. Call `RefundRequest.Submit(...)` factory to create the domain aggregate (calculates `refundAmount = orderAmount * refundPercent / 100`).
9. Upload each image via `IFileUploader.UploadAsync()` and attach `RefundRequestImage` entities to the aggregate.
10. Persist in a transaction. On `DbUpdateException` (duplicate) roll back and return "already has a pending/approved refund request".
11. Return `CreatedAtAction` pointing to `GET /api/refunds/{id}`.

---

## `GET /api/refunds`
**Auth:** Authenticated user (`[Authorize]`)

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `pageNumber` | `int` (query) | No | Page index (default: 1, min: 1) |
| `pageSize` | `int` (query) | No | Items per page (default: 10, max: 100) |
| `status` | `int` (query) | No | Filter by status: 1 = Pending, 2 = Approved, 3 = Rejected |

**Validation Rules:**
- `status`: if provided, must be a valid `RefundRequestStatus` enum value (1-3)

**Response 200 OK:**
```json
{
  "isSuccess": true,
  "message": "Refund requests retrieved successfully.",
  "value": {
    "items": [
      {
        "id": "guid",
        "orderId": "guid",
        "policyName": "string",
        "refundPercent": 25.0,
        "orderAmount": 100000.0,
        "refundAmount": 25000.0,
        "status": "Pending",
        "imageCount": 2,
        "createdAtUtc": "2026-06-10T12:00:00Z",
        "reviewedAtUtc": null
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

**Error Responses:**
| Status | Condition |
|--------|-----------|
| `400` | Invalid status enum value |

**Handler Logic:**
1. Resolve `userId` from `ICurrentUserService`.
2. Query `RefundRequest` where `IsDeleted == false && UserId == userId`.
3. If `status` is provided, validate the enum value, then filter by `(int)refund.Status == status`.
4. Eager-load `Images` collection.
5. Order by `CreatedAtUtc` descending.
6. Apply pagination (skip/take using `GetSkipCount()`).
7. Project to `GetMyRefundRequestsResponse` including computed `ImageCount` (non-deleted images).
8. Wrap in `PaginatedList<T>` and return.

---

## `GET /api/refunds/{id:guid}`
**Auth:** Authenticated user (`[Authorize]`)

**Route Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | `Guid` | Yes | Refund request ID |

**Response 200 OK:**
```json
{
  "isSuccess": true,
  "message": "Refund request retrieved successfully.",
  "value": {
    "id": "guid",
    "orderId": "guid",
    "policyCode": "string",
    "policyName": "string",
    "refundPercent": 25.0,
    "orderAmount": 100000.0,
    "refundAmount": 25000.0,
    "description": "Item was spoiled.",
    "status": "Approved",
    "images": [
      {
        "id": "guid",
        "imageUrl": "https://cdn.example.com/refunds/abc.jpg",
        "fileName": "spoiled_item.jpg"
      }
    ],
    "reviewedBy": "guid",
    "reviewedAtUtc": "2026-06-11T08:00:00Z",
    "rejectionReason": null,
    "walletTransactionId": "guid",
    "createdAtUtc": "2026-06-10T12:00:00Z"
  }
}
```

**Error Responses:**
| Status | Condition |
|--------|-----------|
| `404` | Refund request not found or does not belong to current user |

**Handler Logic:**
1. Resolve `userId` from `ICurrentUserService`.
2. Query `RefundRequest` where `IsDeleted == false && Id == id && UserId == userId`.
3. Eager-load `Images` collection (only non-deleted images projected in response).
4. If not found, return 404.
5. Project to `GetRefundRequestByIdResponse` including image details, review info, and optional `WalletTransactionId`.

---

## `GET /api/manager/refunds`
**Auth:** `Manager` or `Admin` role (`[Authorize(Roles = "Manager,Admin")]`)

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `pageNumber` | `int` (query) | No | Page index (default: 1, min: 1) |
| `pageSize` | `int` (query) | No | Items per page (default: 10, max: 100) |
| `status` | `int` (query) | No | Filter by status: 1 = Pending, 2 = Approved, 3 = Rejected |

**Validation Rules:**
- `status`: if provided, must be a valid `RefundRequestStatus` enum value (1-3)

**Response 200 OK:**
```json
{
  "isSuccess": true,
  "message": "Refund requests retrieved successfully.",
  "value": {
    "items": [
      {
        "id": "guid",
        "orderId": "guid",
        "userId": "guid",
        "policyName": "string",
        "refundPercent": 25.0,
        "orderAmount": 100000.0,
        "refundAmount": 25000.0,
        "status": "Pending",
        "imageCount": 2,
        "createdAtUtc": "2026-06-10T12:00:00Z",
        "reviewedAtUtc": null
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

**Error Responses:**
| Status | Condition |
|--------|-----------|
| `400` | Invalid status enum value |

**Handler Logic:**
1. Query all non-deleted `RefundRequest` records (no user filter — manager sees all).
2. Same filtering, pagination, and projection as `GET /api/refunds` but response includes `UserId`.
3. Order by `CreatedAtUtc` descending.

---

## `GET /api/manager/refunds/{id:guid}`
**Auth:** `Manager` or `Admin` role (`[Authorize(Roles = "Manager,Admin")]`)

**Route Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | `Guid` | Yes | Refund request ID |

**Response 200 OK:**
```json
{
  "isSuccess": true,
  "message": "Refund request retrieved successfully.",
  "value": {
    "id": "guid",
    "orderId": "guid",
    "userId": "guid",
    "policyCode": "string",
    "policyName": "string",
    "refundPercent": 25.0,
    "orderAmount": 100000.0,
    "refundAmount": 25000.0,
    "description": "Item was spoiled.",
    "status": "Pending",
    "images": [
      {
        "id": "guid",
        "imageUrl": "https://cdn.example.com/refunds/abc.jpg",
        "fileName": "spoiled_item.jpg"
      }
    ],
    "reviewedBy": null,
    "reviewedAtUtc": null,
    "rejectionReason": null,
    "walletTransactionId": null,
    "createdAtUtc": "2026-06-10T12:00:00Z"
  }
}
```

**Error Responses:**
| Status | Condition |
|--------|-----------|
| `404` | Refund request not found |

**Handler Logic:**
1. Query `RefundRequest` where `IsDeleted == false && Id == id` (no user scoping — manager sees all).
2. Eager-load `Images` collection.
3. If not found, return 404.
4. Project to `GetRefundRequestDetailResponse` (same shape as user detail but includes `UserId`).

---

## `POST /api/manager/refunds/{id:guid}/approve`
**Auth:** `Manager` or `Admin` role (`[Authorize(Roles = "Manager,Admin")]`)

**Route Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | `Guid` | Yes | Refund request ID to approve |

**Response 200 OK:**
```json
{
  "isSuccess": true,
  "message": "Refund request approved and wallet credited successfully.",
  "value": {
    "id": "guid",
    "walletTransactionId": "guid",
    "refundAmount": 25000.0,
    "balanceAfter": 150000.0,
    "status": "Approved"
  }
}
```

**Error Responses:**
| Status | Condition |
|--------|-----------|
| `400` | Refund request not found |
| `400` | Refund request is no longer pending (already Approved/Rejected) |
| `400` | Refund request user not found |
| `400` | Invalid operation (thrown by domain `RefundRequest.Approve()`) |
| `500` | Unexpected server error |

**Handler Logic:**
1. Begin transaction and acquire a row-level lock on the refund request (`LockRefundRequestAsync`).
2. Load the `RefundRequest` by ID — fail if null or soft-deleted.
3. Ensure its `Status == Pending` — fail if already Approved/Rejected.
4. Acquire a row-level lock on the user row (`LockUserAsync`).
5. Load the `User` — fail if null or soft-deleted.
6. Record `balanceBefore = user.Balance.Amount`.
7. Compute `balanceAfter = balanceBefore + refund.RefundAmount`.
8. Update `user.Balance` to the new amount.
9. Create a `WalletTransaction` of type `Refund` with the before/after balances.
10. Call `refund.Approve(reviewerId, walletTransaction.Id)` — transitions status to `Approved`.
11. Persist refund, user, and wallet transaction in a single transaction.
12. Return the approval result including the transaction ID and new balance.

---

## `POST /api/manager/refunds/{id:guid}/reject`
**Auth:** `Manager` or `Admin` role (`[Authorize(Roles = "Manager,Admin")]`)

**Content-Type:** `application/json`

**Route Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | `Guid` | Yes | Refund request ID to reject |

**Request Body:**
```json
{
  "reason": "Images do not clearly show the defect."
}
```

**Validation Rules:**
- `Id`: must not be empty
- `Reason`: must not be empty, max 1000 characters

**Response 200 OK:**
```json
{
  "isSuccess": true,
  "message": "Refund request rejected successfully.",
  "value": {
    "id": "guid",
    "status": "Rejected",
    "rejectionReason": "Images do not clearly show the defect."
  }
}
```

**Error Responses:**
| Status | Condition |
|--------|-----------|
| `400` | Refund request not found |
| `400` | Refund request is no longer pending |
| `400` | Invalid operation (thrown by domain `RefundRequest.Reject()`) |
| `500` | Unexpected server error |

**Handler Logic:**
1. Begin transaction and acquire a row-level lock on the refund request (`LockRefundRequestAsync`).
2. Load the `RefundRequest` by ID — fail if null or soft-deleted.
3. Ensure its `Status == Pending` — fail if already Approved/Rejected.
4. Call `refund.Reject(reviewerId, reason)` — transitions status to `Rejected` and records the rejection reason.
5. Persist in a single transaction.
6. Return the rejection result with the recorded reason.

---

## Common Envelope

All endpoints return a standard `Result<T>` / `Result` wrapper:

```json
{
  "isSuccess": true,
  "message": "Human-readable message",
  "value": { ... },
  "error": null
}
```

On failure:

```json
{
  "isSuccess": false,
  "message": "Error description",
  "value": null,
  "error": {
    "code": "ErrorCode",
    "message": "Error description"
  }
}
```

## RefundRequestStatus Enum

| Value | Name |
|-------|------|
| `1` | Pending |
| `2` | Approved |
| `3` | Rejected |

## Pagination Parameters

| Parameter | Default | Min | Max |
|-----------|---------|-----|-----|
| `pageNumber` | `1` | `1` | — |
| `pageSize` | `10` | `1` | `100` |

## PaginatedList Response Shape

```json
{
  "items": [ ... ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 42,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```
