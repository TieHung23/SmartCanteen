# SmartCanteen API Documentation

Base URL: `/api`  
API Version: `1.0`  
All timestamps: `DateTimeOffset` (ISO 8601)  
Currency: **Point** (no currency field exposed)

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

## Auth

### `POST /api/auth/register`
**Auth:** AllowAnonymous  

**Request body:**
```json
{
  "name": "string",
  "email": "string",
  "password": "string",
  "studentId": "string | null",
  "dateOfBirth": "2024-01-15 | null",
  "majorOrClass": "string | null",
  "phoneNumber": "string | null",
  "address": "string | null",
  "gender": 0 | null
}
```
Gender: `0=Male, 1=Female, 2=Other`

**201 Response:**
```json
{
  "value": {
    "userId": "guid"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `GET /api/auth/verify-email`
**Auth:** AllowAnonymous  
**Query:** `?token=string`

**Response:**
```json
{
  "value": {
    "message": "string"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/auth/login`
**Auth:** AllowAnonymous  

**Request body:**
```json
{
  "email": "string",
  "password": "string"
}
```

**Response:**
```json
{
  "value": {
    "accessToken": "string",
    "accessTokenExpiresAt": "2024-01-01T00:00:00Z",
    "refreshToken": "string",
    "refreshTokenExpiresAt": "2024-01-01T00:00:00Z"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/auth/google`
**Auth:** AllowAnonymous  

**Request body:**
```json
{
  "idToken": "string"
}
```

**Response:** Same `AuthTokensDto` as login.

### `POST /api/auth/refresh`
**Auth:** AllowAnonymous  

**Request body:**
```json
{
  "refreshToken": "string"
}
```

**Response:** Same `AuthTokensDto` as login.

### `POST /api/auth/logout`
**Auth:** Authorize  

**Request body:**
```json
{
  "refreshToken": "string"
}
```

**Response:**
```json
{
  "value": {
    "message": "string"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `GET /api/auth/me`
**Auth:** Authorize  

**Response:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "email": "string",
    "imgUrl": "string | null",
    "role": 0,
    "status": 0,
    "emailVerified": true,
    "studentId": "string | null",
    "dateOfBirth": "2024-01-15 | null",
    "majorOrClass": "string | null",
    "phoneNumber": "string | null",
    "address": "string | null",
    "gender": 0 | null,
    "balanceAmount": 0.0,
    "lastLoginAt": "2024-01-01T00:00:00Z | null"
  },
  "isSuccess": true,
  "message": "string"
}
```
Role: `0=User, 1=Admin`  
AccountStatus: `0=Active, 1=Disabled`

---

## Categories

### `GET /api/categories`
**Auth:** Authorize  
**Query:** `?name=string&pageNumber=1&pageSize=10`

**Paginated response items:**
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "imgUrl": "string | null"
}
```

### `GET /api/categories/{id}`
**Auth:** Authorize  

**Response:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "imgUrl": "string | null",
    "createdAtUtc": "2024-01-01T00:00:00Z",
    "updatedAtUtc": "2024-01-01T00:00:00Z | null",
    "createdBy": "guid",
    "updatedBy": "guid"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/categories`
**Auth:** Authorize  
**Content-Type:** `multipart/form-data`

**Fields:**
- `name` (string)
- `description` (string)
- `image` (file, optional) — uploaded to Cloudinary, URL saved as `imgUrl`

**201 Response:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "imgUrl": "string | null"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `PUT /api/categories/{id}`
**Auth:** Authorize  
**Content-Type:** `multipart/form-data`

**Fields:**
- `name` (string)
- `description` (string)
- `image` (file, optional) — uploaded to Cloudinary, URL saved as `imgUrl`; if omitted, existing `imgUrl` is preserved

**Response:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "imgUrl": "string | null"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `DELETE /api/categories/{id}` (soft delete)
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

## Dishes

### `GET /api/dishes`
**Auth:** Authorize  
**Query:** `?name=string&categoryId=guid&isActive=bool&pageNumber=1&pageSize=10`

**Paginated response items:**
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "price": 0.0,
  "isActive": true,
  "categoryId": "guid",
  "imgUrl": "string | null"
}
```

### `GET /api/dishes/{id}`
**Auth:** Authorize  

**Response:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "price": 0.0,
    "isActive": true,
    "categoryId": "guid",
    "imgUrl": "string | null"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/dishes`
**Auth:** Authorize  
**Content-Type:** `multipart/form-data`

**Fields:**
- `name` (string)
- `description` (string)
- `price` (decimal)
- `categoryId` (guid)
- `image` (file, optional) — uploaded to Cloudinary, URL saved as `imgUrl`

**201 Response:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "price": 0.0,
    "isActive": true,
    "categoryId": "guid",
    "imgUrl": "string | null"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `PUT /api/dishes/{id}`
**Auth:** Authorize  
**Content-Type:** `multipart/form-data`

**Fields:**
- `name` (string)
- `description` (string)
- `price` (decimal)
- `isActive` (bool)
- `categoryId` (guid)
- `image` (file, optional) — uploaded to Cloudinary, URL saved as `imgUrl`; if omitted, existing `imgUrl` is preserved

**Response:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "price": 0.0,
    "isActive": true,
    "categoryId": "guid",
    "imgUrl": "string | null"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `DELETE /api/dishes/{id}`
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

## Meals

### `GET /api/meals`
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
  "dishes": [
    { "dishId": "guid", "quantity": 1 }
  ]
}
```

### `GET /api/meals/{id}`
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
    "mealTemplates": [
      {
        "name": "string",
        "settings": [
          { "categoryId": "guid", "minQuantity": 1, "maxQuantity": 3, "isRequired": true }
        ]
      }
    ],
    "dishes": [
      { "dishId": "guid", "quantity": 1 }
    ]
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/meals`
**Auth:** Authorize  

**Request body:**
```json
{
  "name": "string",
  "description": "string",
  "availableFrom": "2024-01-01T00:00:00Z",
  "availableTo": "2024-01-01T00:00:00Z",
  "availableForOrder": "2024-01-01T00:00:00Z",
  "mealTemplates": [
    {
      "name": "string",
      "settings": [
        { "categoryId": "guid", "minQuantity": 1, "maxQuantity": 3, "isRequired": true }
      ]
    }
  ],
  "dishes": [
    { "dishId": "guid", "quantity": 1 }
  ]
}
```

**201 Response:**
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

### `PUT /api/meals/{id}`
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
        { "categoryId": "guid", "minQuantity": 1, "maxQuantity": 3, "isRequired": true }
      ]
    }
  ],
  "dishes": [
    { "dishId": "guid", "quantity": 1 }
  ]
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

### `DELETE /api/meals/{id}`
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
  "mealId": "guid",
  "items": [
    { "dishId": "guid", "quantity": 1 }
  ]
}
```

**201 Response:**
```json
{
  "value": {
    "id": "guid",
  "transactionId": "guid",
  "totalPrice": 0.0,
  "message": "string",
  "userRemainingBalance": 0.0
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

## Payments

### `GET /api/payments/{id}`
**Auth:** Authorize  

**Response:**
```json
{
  "value": {
    "paymentId": "guid",
    "userId": "guid",
    "gatewayOrderId": "string",
    "gatewayTransactionId": "string | null",
    "amountVnd": 0.0,
    "convertedPoints": 0.0,
    "method": 0,
    "type": 0,
    "status": "string",
    "failureReason": "string | null",
    "createdAtUtc": "2024-01-01T00:00:00Z",
    "completedAtUtc": "2024-01-01T00:00:00Z | null"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/payments/top-up`
**Auth:** Authorize  

**Request body:**
```json
{
  "amountVnd": 0.0,
  "method": 0
}
```

**Response:**
```json
{
  "value": {
    "paymentId": "guid",
    "amountVnd": 0.0,
    "convertedPoints": 0.0,
    "method": 0,
    "status": "string",
    "gatewayOrderId": "string",
    "paymentContent": "string",
    "payUrl": "string | null"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/payments/sepay/ipn`
**Auth:** AllowAnonymous  
**Body:** Raw `JsonElement` object (SePay webhook payload)

**Response:**
```json
{
  "success": true
}
```

---

## WalletTransaction

WalletTransaction records every balance change in a user's wallet. There is no dedicated CRUD endpoint — WalletTransactions are created as side effects of other flows.

### Entity Schema

| Field | Type | Description |
|---|---|---|
| `id` | `guid` | Primary key |
| `userId` | `guid` | FK → User |
| `amount` | `decimal` | Positive for credit (TopUp), negative for debit (OrderPayment) |
| `balanceBefore` | `decimal` | User's wallet balance before this transaction |
| `balanceAfter` | `decimal` | User's wallet balance after this transaction |
| `transactionType` | `int` | `1=TopUp`, `2=OrderPayment`, `3=Refund` |
| `paymentId` | `guid \| null` | FK → Payment (only set for top-up transactions) |
| `createdAtUtc` | `datetime` | |

### Relationship to Order

Each **Order** references exactly one `WalletTransaction` (`transactionId` field). When an order is placed:
- The system deducts the total price from the user's wallet
- A `WalletTransaction` (type `OrderPayment`) is created with `amount = -totalPrice`
- The order stores the `transactionId`

### Relationship to Payment

Each **top-up Payment** may produce a `WalletTransaction` (type `TopUp`) when the SePay IPN webhook confirms the transaction. The `paymentId` field on the WalletTransaction links back to the originating Payment.

### Creation Flows

| Flow | Actor | Transaction Type | Amount Sign |
|---|---|---|---|
| Order placed | `POST /api/orders` | `OrderPayment` | Negative (debit) |
| Top-up confirmed via SePay | `POST /api/payments/sepay/ipn` | `TopUp` | Positive (credit) |
| Refund approved | `POST /api/manager/refunds/{id}/approve` | `Refund` | Positive (credit) |

---

## Refund Policies

Refund policies are stored internally as multiple `Settings` rows grouped by `Group` and `Scope`. The frontend must use the dedicated policy APIs instead of reading or modifying individual rows.

### `GET /api/refund-policies`
**Auth:** Authorize

Only active, valid policies are returned.

**Response:**
```json
{
  "value": [
    {
      "code": "MISSING_ITEM",
      "name": "Missing item",
      "description": "Refund when an order is missing an item",
      "percent": 5,
      "requiresImage": true
    }
  ],
  "isSuccess": true,
  "isFailure": false,
  "message": "Refund policies retrieved successfully.",
  "error": {}
}
```

The frontend submits `code` as `policyCode`. The user must not submit the percentage or refund amount.

### `GET /api/manager/refund-policies`
**Auth:** Manager

Returns the same policy DTO as `GET /api/refund-policies`.

### `POST /api/manager/refund-policies`
**Auth:** Manager

**Request body:**

```json
{
  "code": "MISSING_ITEM",
  "name": "Missing item",
  "description": "Refund when an order is missing an item",
  "percent": 5,
  "requiresImage": true
}
```

The backend creates these internal setting rows atomically:

```text
REFUND_POLICY | MISSING_ITEM | NAME           | Missing item | string
REFUND_POLICY | MISSING_ITEM | DESCRIPTION    | Refund...    | string
REFUND_POLICY | MISSING_ITEM | PERCENT        | 5            | decimal
REFUND_POLICY | MISSING_ITEM | REQUIRES_IMAGE | true         | bool
```

### `PUT /api/manager/refund-policies/{code}`
**Auth:** Manager

Example: `PUT /api/manager/refund-policies/MISSING_ITEM`

**Request body:**

```json
{
  "name": "Missing item",
  "description": "Refund when an order is missing an item",
  "percent": 10,
  "requiresImage": true
}
```

All rows in the policy scope are updated in one database transaction.

### `DELETE /api/manager/refund-policies/{code}`
**Auth:** Manager

Soft-deletes all setting rows in the policy scope. Existing refund requests remain unchanged because they store snapshot values.

---

## User Refund Requests

All endpoints below require an authenticated user.

RefundRequestStatus: `1=Pending`, `2=Approved`, `3=Rejected`.

### `POST /api/refunds`
**Auth:** Authorize
**Content-Type:** `multipart/form-data`

**Form fields:**

| Field | Type | Required | Description |
|---|---|---|---|
| `orderId` | `guid` | Yes | Order belonging to the current user |
| `policyCode` | `string` | Yes | Code returned by `GET /api/refund-policies` |
| `description` | `string` | Yes | User's explanation |
| `images` | `file[]` | Conditional | Required when `requiresImage=true`; maximum 5 images |

Example frontend request:

```javascript
const formData = new FormData();
formData.append("orderId", orderId);
formData.append("policyCode", policyCode);
formData.append("description", description);
images.forEach((image) => formData.append("images", image));

await fetch("/api/refunds", {
  method: "POST",
  headers: {
    Authorization: `Bearer ${accessToken}`
  },
  body: formData
});
```

Do not manually set `Content-Type` when using `FormData`; the browser adds the multipart boundary.

**201 Response:**
```json
{
  "value": {
    "id": "refund-request-guid",
    "orderId": "order-guid",
    "policyCode": "MISSING_ITEM",
    "policyName": "Missing item",
    "refundPercent": 5,
    "orderAmount": 100,
    "refundAmount": 5,
    "status": "Pending",
    "imageUrls": [
      "https://example.com/refund-image.jpg"
    ],
    "createdAtUtc": "2026-06-09T10:00:00Z"
  },
  "isSuccess": true,
  "isFailure": false,
  "message": "Refund request submitted successfully.",
  "error": {}
}
```

The backend calculates and snapshots the policy and order values. Later policy changes do not affect an existing request.

Common `400` cases:

- Order does not belong to the current user.
- Order already has a `Pending` or `Approved` refund request.
- Policy is missing, deleted, or invalid.
- A required image is missing.
- An uploaded file is invalid.

### `GET /api/refunds`
**Auth:** Authorize
**Query:** `?status=1&pageNumber=1&pageSize=10`

`status` is optional and uses the numeric RefundRequestStatus values.

**Paginated response item:**
```json
{
  "id": "refund-request-guid",
  "orderId": "order-guid",
  "policyName": "Missing item",
  "refundPercent": 5,
  "orderAmount": 100,
  "refundAmount": 5,
  "status": "Pending",
  "imageCount": 1,
  "createdAtUtc": "2026-06-09T10:00:00Z",
  "reviewedAtUtc": null
}
```

### `GET /api/refunds/{id}`
**Auth:** Authorize

`id` is the Refund Request ID. The current user can only retrieve their own request.

**Response:**
```json
{
  "value": {
    "id": "refund-request-guid",
    "orderId": "order-guid",
    "policyCode": "MISSING_ITEM",
    "policyName": "Missing item",
    "refundPercent": 5,
    "orderAmount": 100,
    "refundAmount": 5,
    "description": "One item was missing from the order",
    "status": "Pending",
    "images": [
      {
        "id": "image-guid",
        "imageUrl": "https://example.com/refund-image.jpg",
        "fileName": "evidence.jpg"
      }
    ],
    "reviewedBy": null,
    "reviewedAtUtc": null,
    "rejectionReason": null,
    "walletTransactionId": null,
    "createdAtUtc": "2026-06-09T10:00:00Z"
  },
  "isSuccess": true,
  "isFailure": false,
  "message": "Refund request retrieved successfully.",
  "error": {}
}
```

---

## Manager Refund Requests

All endpoints below require `Role=Manager` or `Role=Admin`.
Prefix: `/api/manager/refunds`

### `GET /api/manager/refunds`
**Auth:** Manager, Admin
**Query:** `?status=1&pageNumber=1&pageSize=10`

**Paginated response item:**
```json
{
  "id": "refund-request-guid",
  "orderId": "order-guid",
  "userId": "user-guid",
  "policyName": "Missing item",
  "refundPercent": 5,
  "orderAmount": 100,
  "refundAmount": 5,
  "status": "Pending",
  "imageCount": 1,
  "createdAtUtc": "2026-06-09T10:00:00Z",
  "reviewedAtUtc": null
}
```

### `GET /api/manager/refunds/{id}`
**Auth:** Manager, Admin

Returns the refund detail with the additional `userId` field.

Important: `{id}` is the Refund Request ID returned by the manager list API, not `orderId`.

### `POST /api/manager/refunds/{id}/approve`
**Auth:** Manager, Admin
**No request body.**

Approval credits the wallet and creates a `WalletTransaction` with type `Refund`.

**Response:**
```json
{
  "value": {
    "id": "refund-request-guid",
    "walletTransactionId": "wallet-transaction-guid",
    "refundAmount": 5,
    "balanceAfter": 150,
    "status": "Approved"
  },
  "isSuccess": true,
  "isFailure": false,
  "message": "Refund request approved and wallet credited successfully.",
  "error": {}
}
```

An already approved or rejected request cannot be approved again.

### `POST /api/manager/refunds/{id}/reject`
**Auth:** Manager, Admin

**Request body:**
```json
{
  "reason": "Insufficient evidence for the refund request"
}
```

`reason` is required and has a maximum length of 1000 characters.

**Response:**
```json
{
  "value": {
    "id": "refund-request-guid",
    "status": "Rejected",
    "rejectionReason": "Insufficient evidence for the refund request"
  },
  "isSuccess": true,
  "isFailure": false,
  "message": "Refund request rejected successfully.",
  "error": {}
}
```

Rejecting a request does not change the wallet or create a WalletTransaction.

---

## Settings

### `GET /api/settings`
**Auth:** Manager
**Query:** `?code=string&name=string&group=string&scope=string&type=string&pageNumber=1&pageSize=10`

**Paginated response items:**
```json
{
  "id": "guid",
  "code": "string",
  "name": "string",
  "description": "string",
  "group": "string",
  "scope": "string",
  "value": "string",
  "type": "string"
}
```

### `GET /api/settings/{id}`
**Auth:** Manager

**Response:**
```json
{
  "value": {
    "id": "guid",
    "code": "string",
    "name": "string",
    "description": "string",
    "group": "string",
    "scope": "string",
    "value": "string",
    "type": "string"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/settings`
**Auth:** Manager

**Request body:**
```json
{
  "code": "string",
  "name": "string",
  "description": "string | null",
  "group": "string",
  "scope": "string",
  "value": "string",
  "type": "string"
}
```

**201 Response:**
```json
{
  "value": {
    "id": "guid",
    "code": "string",
    "name": "string",
    "description": "string",
    "group": "string",
    "scope": "string",
    "value": "string",
    "type": "string"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `PUT /api/settings/{id}`
**Auth:** Manager

**Request body:**
```json
{
  "name": "string",
  "description": "string | null",
  "group": "string",
  "scope": "string",
  "value": "string",
  "type": "string"
}
```

**Response:**
```json
{
  "value": {
    "id": "guid",
    "code": "string",
    "name": "string",
    "description": "string",
    "group": "string",
    "scope": "string",
    "value": "string",
    "type": "string"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `DELETE /api/settings/{id}`
**Auth:** Manager

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

Rows with `group=REFUND_POLICY` are read-only through the generic Settings API. Use `/api/manager/refund-policies` to create, update, or delete a complete policy.

---

## Verification

### `POST /api/verification/submit`
**Auth:** Authorize  
**Content-Type:** `multipart/form-data`

**Fields:**
- `files`: `List<IFormFile>` — the uploaded documents
- `documentTypes`: `List<int>` — one per file, `0=StudentCard, 1=Transcript, 2=Other`

**201 Response:**
```json
{
  "value": "guid",
  "isSuccess": true,
  "message": "string"
}
```

### `GET /api/verification/me`
**Auth:** Authorize  

**Response:**
```json
{
  "value": {
    "requestId": "guid | null",
    "status": 0,
    "submittedAt": "2024-01-01T00:00:00Z | null",
    "reviewedAt": "2024-01-01T00:00:00Z | null",
    "rejectionReason": "string | null",
    "hasOpenRequest": true
  },
  "isSuccess": true,
  "message": "string"
}
```
VerificationStatus: `0=Pending, 1=Approved, 2=Rejected, 3=Expired`

---

## Admin – Verification

All endpoints below require `Role=Admin`.  
Prefix: `/api/admin/verifications`

### `GET /api/admin/verifications`
**Auth:** Admin  
**Query:** `?pageNumber=1&pageSize=10`

**Paginated response items:**
```json
{
  "id": "guid",
  "userId": "guid",
  "userEmail": "string",
  "userName": "string",
  "submittedAt": "2024-01-01T00:00:00Z",
  "documentCount": 0
}
```

### `GET /api/admin/verifications/{id}`
**Auth:** Admin  

**Response:**
```json
{
  "value": {
    "id": "guid",
    "userId": "guid",
    "userEmail": "string",
    "userName": "string",
    "studentId": "string | null",
    "majorOrClass": "string | null",
    "dateOfBirth": "2024-01-15 | null",
    "status": 0,
    "submittedAt": "2024-01-01T00:00:00Z",
    "reviewedAt": "2024-01-01T00:00:00Z | null",
    "reviewedBy": "guid | null",
    "rejectionReason": "string | null",
    "expiresAt": "2024-01-01T00:00:00Z",
    "documents": [
      {
        "id": "guid",
        "documentType": 0,
        "cloudinaryUrl": "string",
        "fileName": "string",
        "fileSize": 0,
        "mimeType": "string",
        "uploadedAt": "2024-01-01T00:00:00Z"
      }
    ]
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/admin/verifications/{id}/approve`
**Auth:** Admin  
**No request body.**

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

### `POST /api/admin/verifications/{id}/reject`
**Auth:** Admin  

**Request body:**
```json
{
  "reason": "string"
}
```

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
