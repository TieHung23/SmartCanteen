# SmartCanteen Refund API

Base URL: `/api/refunds` (user) / `/api/manager/refunds` (manager)  
API Version: `1.0`  
Currency: **Point**

RefundRequestStatus: `1=Pending, 2=Approved, 3=Rejected`

---

## `POST /api/refunds`
Auth: `[Authorize]`  
Content-Type: `multipart/form-data`

**Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `orderId` | `Guid` | Yes | The order to refund |
| `policyCode` | `string` | Yes | Refund policy scope code (e.g. `"quality"`, `"late_delivery"`) |
| `description` | `string` | Yes | Reason for the refund |
| `images` | `List<IFormFile>` | Conditional | Image evidence; required if the policy mandates it (max 5 files) |

**Validation:** max 5 image files, MIME must start with `image/`.

**201:**
```json
{
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
    "createdAtUtc": "..."
  },
  "isSuccess": true,
  "message": "Refund request submitted successfully."
}
```

---

## `GET /api/refunds`
Auth: `[Authorize]`  
**Query:** `?pageNumber=1&pageSize=10&status=1`

Scoped to current user.

```json
{
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
        "createdAtUtc": "...",
        "reviewedAtUtc": null
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  },
  "isSuccess": true,
  "message": "Refund requests retrieved successfully."
}
```

---

## `GET /api/refunds/{id}`
Auth: `[Authorize]`  
`404` if not found or not owned by current user.

```json
{
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
    "reviewedAtUtc": "...",
    "rejectionReason": null,
    "walletTransactionId": "guid",
    "createdAtUtc": "..."
  },
  "isSuccess": true,
  "message": "Refund request retrieved successfully."
}
```

---

## `GET /api/manager/refunds`
Auth: `[Authorize(Roles = "Manager")]`  
Same pagination/filtering as user endpoint. Response includes `userId` per item.

---

## `GET /api/manager/refunds/{id}`
Auth: `[Authorize(Roles = "Manager")]`  
Same detail shape as user endpoint but includes `userId`.

---

## `POST /api/manager/refunds/{id}/approve`
Auth: `[Authorize(Roles = "Manager")]`

```json
{
  "value": {
    "id": "guid",
    "walletTransactionId": "guid",
    "refundAmount": 25000.0,
    "balanceAfter": 150000.0,
    "status": "Approved"
  },
  "isSuccess": true,
  "message": "Refund request approved and wallet credited successfully."
}
```

**Errors:** `400` — not found or not pending.

---

## `POST /api/manager/refunds/{id}/reject`
Auth: `[Authorize(Roles = "Manager")]`

```json
{ "reason": "Images do not clearly show the defect." }
```

```json
{
  "value": {
    "id": "guid",
    "status": "Rejected",
    "rejectionReason": "Images do not clearly show the defect."
  },
  "isSuccess": true,
  "message": "Refund request rejected successfully."
}
```

**Errors:** `400` — not found, not pending, or empty reason.
