# PaymentsController — Sequence Diagrams

Base route: **`/api/payments`** · API version `1.0`
Source: [`PaymentsController.cs`](../SC.Api/Controllers/PaymentsController.cs)

| Endpoint | Auth | Handler | Purpose |
|---|---|---|---|
| `GET /api/payments/top-up-policy` | JWT | `GetTopUpPolicyQueryHandler` | `vndPerPoint`, min/max amount, currency, point name |
| `GET /api/payments/{id}` | JWT | `GetPaymentByIdQueryHandler` | poll one payment's status |
| `POST /api/payments/top-up` | JWT | `TopUpWalletCommandHandler` | create a `Pending` payment + VietQR |
| `POST /api/payments/sepay/ipn` | **anonymous** | `HandleSepayIpnCommandHandler` | SePay webhook — credits the wallet |

Wallet top-up is **two-legged**: the API creates a `Pending` payment and a QR image, the student transfers the money in their banking app, and SePay calls back over an HMAC-signed webhook that actually credits the balance. The API never sees the bank transaction directly.

**Layers:** `PaymentsController → ISePayWebhookVerifier / IMediator → Handler → IPaymentService → IWalletDomainService / IGenericRepository → SmartCanteenDbContext → PostgreSQL`

**Configuration this flow depends on**

| Source | Keys |
|---|---|
| `Settings` table | `VND_PER_POINT`, `MIN_TOPUP_AMOUNT`, `MAX_TOPUP_AMOUNT` — missing values fail the call with `Payment setting {code} is missing.` |
| `appsettings` `SePay` section | `WebhookSecret`, `BankName`, `BankAccountNumber`, `BankAccountName`, `PaymentCodePrefix`, `WebhookTimestampToleranceSeconds`, `QrTemplate` |

---

## 1. `GET /api/payments/top-up-policy` — read the limits

```plantuml
@startuml
title sd GetTopUpPolicy — GET /api/payments/top-up-policy
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "student : Student" as STU
box "API" #F7F7F7
  participant "ctrl : PaymentsController" as CTRL
  participant "h : GetTopUpPolicyQueryHandler" as H
end box
participant "svc : PaymentService" as PSVC
database "db : PostgreSQL" as DB

activate STU
STU -> CTRL ++: GET /api/payments/top-up-policy
CTRL -> H ++: Send(GetTopUpPolicyQuery) via IMediator
H -> PSVC ++: GetTopUpPolicyAsync()

loop for VND_PER_POINT, MIN_TOPUP_AMOUNT, MAX_TOPUP_AMOUNT
  PSVC -> DB ++: Settings.FirstOrDefaultAsync(s => !s.IsDeleted && s.Code == code)
  DB -->> PSVC --: setting | null
  opt [setting missing | not a positive decimal]
    PSVC -->> H: Failure(InvalidValue, "Payment setting {code} is missing / is invalid.")
    H -->> CTRL: 400 Bad Request
  end
end

opt [min > max]
  PSVC -->> H: Failure(InvalidValue, "Payment top-up min amount cannot be greater than max amount.")
  H -->> CTRL: 400 Bad Request
end

PSVC -->> H --: TopUpPolicyResult(vndPerPoint, min, max, "VND", "Point")
H -->> CTRL --: Success(GetTopUpPolicyResponse)
CTRL -->> STU --: 200 OK
deactivate STU
@enduml
```

---

## 2. `POST /api/payments/top-up` — create the pending payment

Nothing is charged here. The handler validates the amount against the policy, generates a payment code, builds a VietQR URL, and stores a `Pending` payment row.

**Request:** `{ "amountVnd": 100000, "method": 1 }` — `method` must be `SePay`; `FluentValidation` requires `amountVnd > 0` and `method` in `1..4`, and the service then rejects any method other than SePay.

```plantuml
@startuml
title sd TopUpWallet — POST /api/payments/top-up
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "student : Student" as STU
box "API" #F7F7F7
  participant "ctrl : PaymentsController" as CTRL
  participant "h : TopUpWalletCommandHandler" as H
end box
participant "svc : PaymentService" as PSVC
database "db : PostgreSQL" as DB

activate STU
STU -> CTRL ++: POST /api/payments/top-up(amountVnd, method)
CTRL -> H ++: Send(TopUpWalletCommand) via IMediator
note right of H: TopUpWalletCommandValidator (FluentValidation)\namountVnd > 0, method in 1..4
H -> PSVC ++: TopUpWalletAsync(amountVnd, method)

PSVC -> DB ++: Settings.Where(s => codes.Contains(s.Code)).ToListAsync()
DB -->> PSVC --: VND_PER_POINT, MIN_TOPUP_AMOUNT, MAX_TOPUP_AMOUNT

opt [amount <= 0 | min > max | amount outside min..max\n| amount % vndPerPoint != 0 | method != SePay]
  PSVC -->> H: Failure(InvalidValue, reason)
  H -->> CTRL: 400 Bad Request
end

PSVC -> DB ++: Users.FirstOrDefaultAsync(u => u.Id == userId)
DB -->> PSVC --: user | null
opt [user is null]
  PSVC -->> H: Failure(NullValue, "User not found.")
  H -->> CTRL: 400 Bad Request
end

opt [SePay bank name / account number / 2..5 char PaymentCodePrefix not configured]
  PSVC -->> H: Failure(InvalidValue, "SePay ... is not configured.")
  H -->> CTRL: 400 Bad Request
end

PSVC -> PSVC: convertedPoints = amountVnd / vndPerPoint\ngatewayOrderId = PREFIX + guid, truncated to 30 chars\npaymentContent = RequiredTransferContentPrefix + gatewayOrderId\npayUrl = https://qr.sepay.vn/img?acc=...&bank=...&amount=...&des=...

critical transaction
  PSVC -> DB ++: BeginTransaction, insert Payment\n(Status = Pending, Type = TopUp, GatewayOrderId = code), Commit
  deactivate DB
end

PSVC -->> H --: TopUpWalletResult
H -->> CTRL --: Success(TopUpWalletResponse)
CTRL -->> STU --: 200 OK { paymentId, payUrl, qrCodeUrl, paymentContent,\nbank details, status: "Pending" }
deactivate STU

note over PSVC, DB: nothing is charged here — the wallet is credited only by the IPN.\nAny exception -> RollbackAsync() + Failure(ServerError) : 400
@enduml
```

The FE then shows the QR and polls `GET /api/payments/{id}` (or waits for the `PaymentCompleted` notification) until the status flips to `Completed`.

---

## 3. `POST /api/payments/sepay/ipn` — the webhook that actually credits

`[AllowAnonymous]`, so **the HMAC signature is the only authentication**. The controller verifies it against the raw request body before any parsing, and the handler is idempotent: a replayed callback returns `200` without crediting twice.

```plantuml
@startuml
title sd HandleSepayIpn — POST /api/payments/sepay/ipn
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "student : Student" as STU
participant "bank : Bank app" as BANK
participant "sepay : SePay gateway" as SEPAY
box "API" #F7F7F7
  participant "ctrl : PaymentsController" as CTRL
  participant "ver : ISePayWebhookVerifier" as VER
  participant "h : HandleSepayIpnCommandHandler" as H
end box
box "Domain services" #FAFAFA
  participant "svc : PaymentService" as PSVC
  participant "wallet : IWalletDomainService" as WAL
end box
database "db : PostgreSQL" as DB
participant "noti : IBusinessNotificationService" as NOTI

activate STU
STU -> BANK: scan the QR, transfer amountVnd with paymentContent in the memo
BANK -> SEPAY: incoming transfer recorded
SEPAY -> CTRL ++: POST /api/payments/sepay/ipn\nraw JSON + X-SePay-Signature + X-SePay-Timestamp

CTRL -> CTRL: EnableBuffering(), read the raw body verbatim
CTRL -> VER ++: Verify(rawBody, signature, timestamp)
VER -> VER: secret configured, both headers present,\ntimestamp within WebhookTimestampToleranceSeconds (default 300),\nHMAC-SHA256(secret, timestamp + "." + rawBody) == signature, fixed-time compare
VER -->> CTRL --: IsValid
opt [signature invalid]
  CTRL -->> SEPAY: 401 Unauthorized — reason echoed only in Development
end
note over CTRL, VER: the endpoint is [AllowAnonymous] —\nthe HMAC signature is the only authentication

opt [body is not a JSON object]
  CTRL -->> SEPAY: 400 { success: false, message }
end
opt [SePay test-mode ping with no "SC..." payment code]
  CTRL -->> SEPAY: 200 { success: true } — ignored, nothing looked up
end

CTRL -> H ++: Send(HandleSepayIpnCommand { Data }) via IMediator
H -> PSVC ++: HandleSepayIpnAsync(data)
PSVC -> PSVC: transferType must be incoming;\nresolve the payment code from "code", else regex it out of "content"
PSVC -> DB ++: Payments.FirstOrDefaultAsync(p => p.GatewayOrderId == code)
DB -->> PSVC --: payment | null

opt [code missing | payment not found | method != SePay]
  PSVC -->> H: Failure(reason)
  H -->> CTRL: Failure
  CTRL -->> SEPAY: 400 { success: false, message }
end
opt [payment.Status == Completed — replayed webhook]
  PSVC -->> H: CompletePaymentResult(WasAlreadyCompleted = true)
  H -->> CTRL: Success — no second credit, no duplicate notification
  CTRL -->> SEPAY: 200 { success: true }
end
opt [gateway transaction id missing | transferAmount != payment.AmountVnd]
  PSVC -->> H: Failure(InvalidValue, reason)
  H -->> CTRL: Failure
  CTRL -->> SEPAY: 400 { success: false, message }
end

critical transaction + per-user row lock
  PSVC -> DB ++: BeginTransaction
  deactivate DB
  PSVC -> WAL ++: LockUserAsync(payment.UserId)
  WAL -> DB ++: SELECT 1 FROM "Users" WHERE "Id" = @userId FOR UPDATE
  deactivate DB
  PSVC -> WAL: TryCreditUserBalanceAsync(userId, payment.ConvertedPoints)
  WAL -> DB ++: UPDATE "Users" SET "Balance_Amount" = "Balance_Amount" + @points\nWHERE "Id" = @userId RETURNING "Balance_Amount"
  DB -->> WAL --: balanceAfter | Failure
  WAL -->> PSVC --: balanceAfter
  opt [credit failed]
    PSVC -> DB: RollbackAsync()
    PSVC -->> H: Failure(NullValue, "Payment user not found.")
    H -->> CTRL: 400 Bad Request
  end
  PSVC -> DB ++: payment.MarkAsCompleted(gatewayTransactionId),\ninsert WalletTransaction (Type = TopUp, PaymentId = payment.Id), Commit
  deactivate DB
end

PSVC -->> H --: CompletePaymentResult(WasAlreadyCompleted = false)
H -> NOTI ++: NotifyAsync(PaymentCompleted, userId, paymentId, { points })
NOTI -->> STU --: SignalR + FCM "wallet topped up"
H -->> CTRL --: Success
CTRL -->> SEPAY --: 200 { success: true }
deactivate STU

note over PSVC, DB: concurrent callbacks are caught by unique indexes, not by the status check:\nIX_WalletTransaction_PaymentId -> re-read, report "already completed" if it is\nIX_Payments_GatewayTransactionId -> "SePay transaction was already processed."
@enduml
```

**Idempotency, in order of defence**

1. `payment.Status == Completed` — the cheap check, catches sequential replays.
2. `IX_WalletTransaction_PaymentId` unique index — catches two callbacks racing inside overlapping transactions; the loser re-reads the payment and, if it is now `Completed`, reports success.
3. `IX_Payments_GatewayTransactionId` unique index — catches one bank transaction being applied to two different payments.

---

## 4. `GET /api/payments/{id}` — poll status

```plantuml
@startuml
title sd GetPaymentById — GET /api/payments/{id}
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "student : Student" as STU
box "API" #F7F7F7
  participant "ctrl : PaymentsController" as CTRL
  participant "h : GetPaymentByIdQueryHandler" as H
end box
database "db : PostgreSQL" as DB

activate STU
STU -> CTRL ++: GET /api/payments/{id}
CTRL -> H ++: Send(GetPaymentByIdQuery(id)) via IMediator
H -> DB ++: Payments.FirstOrDefaultAsync(p => p.Id == id)
DB -->> H --: payment | null
opt [not found]
  H -->> CTRL: Failure(NullValue) : 404
end
H -->> CTRL --: Success(status, amountVnd, convertedPoints)
CTRL -->> STU --: 200 OK
deactivate STU

note over STU, DB: the FE polls this until Status flips to Completed,\nor waits for the PaymentCompleted notification instead
@enduml
```

---

## Payment state

```plantuml
@startuml
title Payment state
hide empty description

[*] --> Pending : POST /api/payments/top-up — QR issued
Pending --> Completed : signed IPN, amount matches, wallet credited
Pending --> Pending : IPN rejected (bad signature, wrong amount, unknown code)
Completed --> Completed : replayed IPN — 200, no second credit
Completed --> [*]
@enduml
```

---

## Related

- [`orders-diagrams.md`](orders-diagrams.md) — where the credited points get spent
- [`refundmanager-diagrams.md`](refundmanager-diagrams.md) — the other writer of `WalletTransaction`
- [`../API-Modules/payment-api.md`](../API-Modules/payment-api.md) — request/response reference
- [`../SETUP.md`](../SETUP.md) — the `SePay` configuration keys this flow needs
