# SmartCanteen Payment API

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

**Required headers:**
```text
X-SePay-Timestamp: Unix timestamp in seconds
X-SePay-Signature: sha256=<HMAC-SHA256 hex digest>
```

The signature is calculated from `{timestamp}.{raw_request_body}` using
`SePay:WebhookSecret`. Requests outside the configured timestamp tolerance are rejected.

**Response:**
```json
{
  "success": true
}
```

---

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
| Refund (future) | — | `Refund` | Positive (credit) |

---

