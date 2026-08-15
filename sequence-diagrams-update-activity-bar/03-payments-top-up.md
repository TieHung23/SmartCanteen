# 3 · POST /api/payments/top-up

Corrected copy of `sd TopUpWallet — POST /api/payments/top-up` from [`../sequence-diagrams/payments-diagrams.md`](../sequence-diagrams/payments-diagrams.md).

## What changed

- DB reply added after «BeginTransaction, insert Payment\n(Status = Pending,»

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
  DB -->> PSVC --: committed
end

PSVC -->> H --: TopUpWalletResult
H -->> CTRL --: Success(TopUpWalletResponse)
CTRL -->> STU --: 200 OK { paymentId, payUrl, qrCodeUrl, paymentContent,\nbank details, status: "Pending" }
deactivate STU

note over PSVC, DB: nothing is charged here — the wallet is credited only by the IPN.\nAny exception -> RollbackAsync() + Failure(ServerError) : 400
@enduml
```
