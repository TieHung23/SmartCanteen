# RefundsController — Sequence Diagrams

Base route: **`/api/refunds`** · Auth: **JWT (any authenticated user)** · API version `1.0`
Source: [`RefundsController.cs`](../SC.Api/Controllers/RefundsController.cs)

| Endpoint | Handler | Notes |
|---|---|---|
| `POST /api/refunds` | `SubmitRefundRequestCommandHandler` | `multipart/form-data` — the only endpoint here that writes |
| `GET /api/refunds` | `GetMyRefundRequestsQueryHandler` | paginated, scoped to the caller |
| `GET /api/refunds/{id}` | `GetRefundRequestByIdQueryHandler` | `404` if not found |

This is the **student-initiated** refund path: submit evidence, wait for a manager. It always lands in `Pending`.

> Not to be confused with the **auto-credited** refunds raised from a change proposal — those skip the manager entirely and land in `Approved` in one call. See [`changeproposals-diagrams.md`](changeproposals-diagrams.md).

**Layers:** `RefundsController → IMediator → SubmitRefundRequestCommandHandler → IFileValidator / IFileUploader (Cloudinary) / IGenericRepository → SmartCanteenDbContext → PostgreSQL`

---

## 1. `POST /api/refunds` — submit a refund request

Order of operations matters here: **every** guard and validation runs *before* any image is uploaded, and the upload happens *before* the transaction opens. An invalid request never reaches Cloudinary, and a Cloudinary failure never leaves a half-written transaction.

```plantuml
@startuml
title sd SubmitRefundRequest — POST /api/refunds
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "student : Student" as STU
box "API" #F7F7F7
  participant "ctrl : RefundsController" as CTRL
  participant "h : SubmitRefundRequestCommandHandler" as H
end box
box "Infrastructure" #FAFAFA
  participant "fval : IFileValidator" as FVAL
  participant "up : IFileUploader" as UP
end box
participant "cloud : Cloudinary" as CLOUD
database "db : PostgreSQL" as DB
participant "noti : IBusinessNotificationService" as NOTI

activate STU
STU -> CTRL ++: POST /api/refunds (multipart: orderId, policyCode, description, images[])
CTRL -> CTRL: map IFormFile[] to RefundImageInput(stream, name, size, contentType)
CTRL -> H ++: Send(SubmitRefundRequestCommand) via IMediator

opt [userId is empty]
  H -->> CTRL: Failure(Forbidden, "Not authenticated.") : 400
end

H -> DB ++: Orders.Include(OrderItems).FirstOrDefaultAsync(o => o.Id == orderId\n&& o.CreatedBy == userId && !o.IsDeleted)
DB -->> H --: order | null
opt [order not found or not owned by the caller]
  H -->> CTRL: Failure(NullValue, "Order not found or does not belong to the current user.") : 400
end

H -> DB ++: RefundRequests.AnyAsync(r => r.OrderId == orderId && !r.IsDeleted\n&& (r.Status == Pending or r.Status == Approved))
DB -->> H --: activeRequestExists
opt [activeRequestExists]
  H -->> CTRL: Failure(InvalidValue, "This order already has a pending or approved refund request.") : 400
end

H -> DB ++: Settings.Where(s => s.Group == "REFUND_POLICY"\n&& s.Scope.ToLower() == policyCode).ToListAsync()
DB -->> H --: NAME / DESCRIPTION / PERCENT / REQUIRES_IMAGE\n-> RefundPolicyDefinition.TryCreate(policyCode, settings)
opt [policy missing or invalid]
  H -->> CTRL: Failure(InvalidValue, "Refund policy is not active or invalid.") : 400
end
opt [policy.RequiresImage and no images were sent]
  H -->> CTRL: Failure(EmptyValue, "At least one image is required for this refund policy.") : 400
end

loop for each uploaded file
  H -> FVAL ++: MimeType starts with "image/", then Validate(fileName, fileSize, mimeType)
  FVAL -->> H --: Result | Failure
  opt [file rejected]
    H -->> CTRL: Failure(UnsupportedFileFormat | size error) : 400
  end
end

opt [orderAmount <= 0]
  H -->> CTRL: Failure(InvalidValue, "Order amount must be greater than zero.") : 400
end

H -> H: refundRequest = RefundRequest.Submit(orderId, userId, policy.Scope,\npolicy.Name, policy.Percent, orderAmount, description) -> Status = Pending

group uploads happen before the transaction opens
  loop for each image
    H -> UP ++: UploadAsync(stream, fileName)
    UP -> CLOUD ++: POST image
    CLOUD -->> UP --: { url }
    UP -->> H --: upload
    opt [upload.Url is blank]
      H -->> CTRL: Failure(ServerError, "Refund image upload failed.") : 400
    end
    H -> H: refundRequest.AddImage(RefundRequestImage.Create(url, fileName, userId))
  end
end

critical transaction
  H -> DB ++: BeginTransaction,\ninsert RefundRequest (Status = Pending, snapshots of policy name / percent / order amount)\n+ RefundRequestImage per uploaded image, Commit
  deactivate DB
end

H -> NOTI ++: NotifyAsync(RefundSubmitted, userId, refundRequest.Id, { refundAmount })
NOTI -->> STU --: SignalR + FCM "refund request submitted"
H -->> CTRL --: Success(SubmitRefundRequestResponse)
CTRL -->> STU --: 201 Created (Location: /api/refunds/{id})
deactivate STU

note over H, DB: the AnyAsync check is not the race guard — the unique index is.\nDbUpdateException -> Rollback + "This order already has a pending or approved refund request."
@enduml
```

**The `ExistsAsync` check is not the race guard.** Two submissions racing on the same order both pass it; the unique index on `RefundRequests` is what stops the second one, and the resulting `DbUpdateException` is rolled back and reported as `This order already has a pending or approved refund request.`

**Why the policy values are snapshotted.** `RefundRequest` stores `PolicyNameSnapshot`, `RefundPercentSnapshot` and `OrderAmountSnapshot` at submit time, so a manager editing the policy later never changes the amount owed on an already-submitted request.

---

## 2. `GET /api/refunds` and `GET /api/refunds/{id}` — read paths

```plantuml
@startuml
title sd ReadMyRefunds — GET /api/refunds and GET /api/refunds/{id}
skinparam responseMessageBelowArrow true
skinparam maxMessageSize 240
hide footbox
autonumber

actor "student : Student" as STU
box "API" #F7F7F7
  participant "ctrl : RefundsController" as CTRL
  participant "h : GetMyRefundRequests / GetRefundRequestById handler" as H
end box
database "db : PostgreSQL" as DB

alt [list]
activate STU
  STU -> CTRL ++: GET /api/refunds?pageNumber=1&pageSize=10&status=...
  CTRL -> H ++: Send(GetMyRefundRequestsQuery) via IMediator
  H -> DB ++: RefundRequests.Where(r => r.UserId == me)\n.OrderByDescending(r => r.CreatedAtUtc).Skip(...).Take(...).ToListAsync()\n+ CountAsync()
  DB -->> H --: page + total
  H -> DB ++: OrderItemChangeProposals.Where(p => proposalIds.Contains(p.Id))\n+ Dishes.Where(d => dishIds.Contains(d.Id))
  DB -->> H --: linked proposals + dish names
  H -->> CTRL --: Success(PaginatedList)
  CTRL -->> STU --: 200 OK
else [detail]
  STU -> CTRL ++: GET /api/refunds/{id}
  CTRL -> H ++: Send(GetRefundRequestByIdQuery(id)) via IMediator
  H -> DB ++: RefundRequests.Include(Images).FirstOrDefaultAsync(r => r.Id == id)\nscoped to the caller
  DB -->> H --: refund + evidence | null
  opt [not found]
    H -->> CTRL: Failure(NullValue) : 404
  end
  H -> DB ++: the linked change proposal and dish, when ChangeProposalId is set
  DB -->> H --: proposal + dish
  H -->> CTRL --: Success(detail)
  CTRL -->> STU --: 200 OK
deactivate STU
end
@enduml
```

---

## Refund request state

```plantuml
@startuml
title Refund request state
hide empty description

[*] --> Pending : POST /api/refunds (student, with evidence)
[*] --> Approved : auto-credit from a change proposal or an expired proposal
Pending --> Approved : POST /api/manager/refunds/{id}/approve — wallet credited
Pending --> Rejected : POST /api/manager/refunds/{id}/reject — reason recorded
Approved --> [*]
Rejected --> [*]
@enduml
```

Only the `Pending → Approved | Rejected` edges are the manager's; see [`refundmanager-diagrams.md`](refundmanager-diagrams.md).

---

## Related

- [`refundmanager-diagrams.md`](refundmanager-diagrams.md) — the approve/reject half of this lifecycle
- [`changeproposals-diagrams.md`](changeproposals-diagrams.md) — the auto-credited entry into `Approved`
- [`API-Modules/refund-api.md`](../API-Modules/refund-api.md) · [`API-Modules/refundpolicy-api.md`](../API-Modules/refundpolicy-api.md)
