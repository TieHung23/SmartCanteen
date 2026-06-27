# SmartCanteen Payment / Wallet API

Base URL: `/api/payments`  
API Version: `1.0`  
Currency: **Point** (amounts in VND are converted at server rate)

PaymentMethod: `1=Momo, 2=ZaloPay, 3=VnPay, 4=SePay, 5=Wallet`  
PaymentStatus: `1=Pending, 2=Completed, 3=Failed`  
PaymentType: `1=TopUp, 2=Subscription, 3=Refund, 4=OrderPayment`

---

## `GET /api/payments/top-up-policy`
Auth: `[Authorize]`

Returns the current top-up conversion rate and amount limits for customer UI validation.

```json
{
  "value": {
    "vndPerPoint": 1000,
    "minTopUpAmount": 10000,
    "maxTopUpAmount": 500000,
    "currency": "VND",
    "pointName": "Point"
  },
  "isSuccess": true,
  "message": "Top-up policy retrieved successfully."
}
```

---

## `GET /api/payments/{id}`
Auth: `[Authorize]`  
`404` if not found.

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
    "createdAtUtc": "...",
    "completedAtUtc": "... | null"
  },
  "isSuccess": true
}
```

---

## `POST /api/payments/top-up`
Auth: `[Authorize]`

```json
{ "amountVnd": 0.0, "method": 0 }
```

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
    "payUrl": "string | null",
    "qrCodeUrl": "string | null",
    "bankName": "string",
    "bankAccountNumber": "string",
    "bankAccountName": "string"
  },
  "isSuccess": true
}
```

---

## `POST /api/payments/sepay/ipn`
AllowAnonymous. SePay Instant Payment Notification webhook.

**Required headers:**
- `X-SePay-Timestamp`: Unix timestamp in seconds
- `X-SePay-Signature`: `sha256=<HMAC-SHA256 hex>` computed from `{timestamp}.{raw_body}` using `SePay:WebhookSecret`

**Body:** Raw JSON object

**Response:**
```json
{ "success": true }
```

**Errors:** `401` — signature invalid; `400` — invalid JSON or processing failure.

---

## `GET /api/wallet-transactions`
Auth: `[Authorize]`  
Scoped to current user. Returns transactions from all sources (top-ups, order payments, refunds).

**Query:** `?transactionType=int&pageNumber=1&pageSize=10`

TransactionType: `1=TopUp, 2=OrderPayment, 3=Refund`

```json
{
  "value": {
    "items": [
      {
        "id": "guid",
        "amount": 100.0,
        "balanceBefore": 500.0,
        "balanceAfter": 600.0,
        "transactionType": 1,
        "transactionTypeName": "TopUp",
        "paymentId": "guid | null",
        "createdAtUtc": "..."
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 5,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "isSuccess": true,
  "message": "Wallet transactions retrieved successfully."
}
```

---

# WalletTransaction

No dedicated CRUD. Created as side effects:

| Flow | TransactionType | Amount |
|------|----------------|--------|
| Order placed (`POST /api/orders`) | `2=OrderPayment` | Negative (debit) |
| Top-up confirmed (SePay IPN) | `1=TopUp` | Positive (credit) |
| Refund approved (`POST /api/manager/refunds/{id}/approve`) | `3=Refund` | Positive (credit) |

Each **Order** links to a `WalletTransaction` via `transactionId`.  
Each top-up **Payment** produces a `WalletTransaction` when SePay IPN confirms it.
