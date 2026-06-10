# Payment API Documentation

Base URL: `/api/payments`

All endpoints require the `Authorization: Bearer <token>` header unless marked `[AllowAnonymous]`.

---

## Response Envelope

Every endpoint wraps its payload in a standard `Result<TValue>` envelope:

```json
{
  "message": "string | null",
  "isSuccess": true,
  "isFailure": false,
  "error": {},
  "value": { ... }
}
```

On failure:

```json
{
  "message": "string | null",
  "isSuccess": false,
  "isFailure": true,
  "error": {},
  "value": null
}
```

> **Note:** The `error` object serialises as `{}` because its properties (`Code`, `Message`, `HttpStatusCode`) are decorated with `[JsonIgnore]`. The API surface relies on the HTTP status code and the top-level `message` field to convey error details.

---

## `GET /api/payments/{id:guid}`
**Auth:** Required (`[Authorize]`)

### Request

| Parameter | Location | Type   | Required | Description           |
|-----------|----------|--------|----------|-----------------------|
| `id`      | Route    | `guid` | Yes      | Payment unique identifier |

### Response 200

```json
{
  "message": "Payment retrieved successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": {},
  "value": {
    "paymentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "gatewayOrderId": "ORDER-12345",
    "gatewayTransactionId": "TXN-ABC-123",
    "amountVnd": 50000.00,
    "convertedPoints": 50000.00,
    "method": 1,
    "type": 1,
    "status": "Pending",
    "failureReason": null,
    "createdAtUtc": "2025-01-01T12:00:00Z",
    "completedAtUtc": null
  }
}
```

### Response 404

```json
{
  "message": "Payment not found.",
  "isSuccess": false,
  "isFailure": true,
  "error": {},
  "value": null
}
```

### Response 500

```json
{
  "message": "An error occurred while retrieving payment.",
  "isSuccess": false,
  "isFailure": true,
  "error": {},
  "value": null
}
```

### Handler Logic

1. Fetches the payment aggregate by `id` from the repository.
2. If the payment does **not** exist OR the payment's `UserId` does **not** match the currently authenticated user (`ICurrentUserService.UserId`), returns `404` with `Error.NullValue`.
3. Maps the aggregate to `GetPaymentByIdResponse` DTO.
4. Catches any unhandled exception, logs it, and returns `500` with `Error.ServerError`.

### Status Codes

| Code | Description                                            |
|------|--------------------------------------------------------|
| 200  | Payment found and owned by the current user            |
| 404  | Payment not found, or not owned by the current user    |
| 500  | Unexpected server error                                |

---

## `POST /api/payments/top-up`
**Auth:** Required (`[Authorize]`)

### Request

```json
{
  "amountVnd": 50000.00,
  "method": 1
}
```

| Field      | Type      | Required | Description                                                                |
|------------|-----------|----------|----------------------------------------------------------------------------|
| `amountVnd` | `number`  | Yes      | Amount in VND. Must be **greater than 0**.                                  |
| `method`    | `integer` | Yes      | Payment method code. Must be between **1 and 4** inclusive.                |

**Possible `method` values:**
| Code | Method    |
|------|-----------|
| 1    | Momo      |
| 2    | ZaloPay   |
| 3    | VnPay     |
| 4    | SePay     |

> Method `5` (Wallet) is **not** accepted for top-up.

### Validation Rules (from `TopUpWalletCommandValidator`)

| Field      | Rule              |
|------------|-------------------|
| `amountVnd` | Must be > 0       |
| `method`    | Must be between 1 and 4 (inclusive) |

### Response 200

```json
{
  "message": "Top-up initiated successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": {},
  "value": {
    "paymentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "amountVnd": 50000.00,
    "convertedPoints": 50000.00,
    "method": 1,
    "status": "Pending",
    "gatewayOrderId": "ORDER-12345",
    "paymentContent": "Top-up via Momo",
    "payUrl": "https://payment-gateway.example.com/pay/abc123"
  }
}
```

### Response 400 (validation or service failure)

```json
{
  "message": "Invalid payment method.",
  "isSuccess": false,
  "isFailure": true,
  "error": {},
  "value": null
}
```

### Response 500

```json
{
  "message": "An error occurred while topping up wallet.",
  "isSuccess": false,
  "isFailure": true,
  "error": {},
  "value": null
}
```

### Handler Logic

1. Delegates to `IPaymentService.TopUpWalletAsync(amountVnd, method)`.
2. If the service returns a failure, propagates the `Error` and `Message` to the caller as `400`.
3. On success, maps `TopUpWalletResult` to `TopUpWalletResponse` DTO (includes `payUrl` for payment gateway redirect).
4. Catches any unhandled exception, logs it, and returns `500` with `Error.ServerError`.

### Status Codes

| Code | Description                                       |
|------|---------------------------------------------------|
| 200  | Top-up initiated; `payUrl` may be present         |
| 400  | Validation failure or business-logic failure      |
| 500  | Unexpected server error                           |

---

## `POST /api/payments/sepay/ipn`
**Auth:** None (`[AllowAnonymous]`)

SePay Instant Payment Notification (IPN) webhook endpoint. SePay sends a POST with a JSON payload whenever a bank transfer is detected.

### Request

The body must be a **JSON object** (any shape). It is converted to a flat `Dictionary<string, string>` where:

- String property values are extracted as-is.
- Non-string values (numbers, booleans, etc.) are converted via `.ToString()`.

```json
{
  "gatewayOrderId": "ORDER-12345",
  "amount": 50000,
  "transferType": "in",
  "accountNumber": "1234567890",
  "transferTime": "2025-01-01 12:00:00",
  "content": "NAP TKKH 12345",
  "transactionId": "TXN-ABC-123"
}
```

> No fixed schema is enforced by the controller; the raw `JsonElement` is passed through as a dictionary. The actual parsing and validation happen inside the service layer (`IPaymentService.HandleSepayIpnAsync`).

### Response 200

```json
{
  "success": true
}
```

### Response 400

```json
{
  "success": false,
  "message": "SePay IPN payload must be a JSON object."
}
```

Or on service failure:

```json
{
  "success": false,
  "message": "Order not found."
}
```

### Handler Logic

1. Validates the incoming `JsonElement` is a JSON object (`ValueKind == JsonValueKind.Object`); if not, returns `400` immediately with a plain object (no `Result` envelope).
2. Converts the JSON object into `Dictionary<string, string>` via `ToDictionary()`.
3. Wraps the dictionary into a `HandleSepayIpnCommand` and sends it through MediatR.
4. The handler delegates to `IPaymentService.HandleSepayIpnAsync(data)`.
5. If the service returns failure, returns `400` with `{ success: false, message: "..." }`.
6. On service success, returns `200` with `{ success: true }`.

### Status Codes

| Code | Description                           |
|------|---------------------------------------|
| 200  | IPN processed successfully            |
| 400  | Invalid payload or service failure    |

---
