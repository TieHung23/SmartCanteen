# 6 · POST /api/refunds

Corrected copy of `sd SubmitRefundRequest — POST /api/refunds` from [`../sequence-diagrams/refunds-diagrams.md`](../sequence-diagrams/refunds-diagrams.md).

## What changed

- DB reply added after «BeginTransaction,\ninsert RefundRequest (Status = Pe»
- NOTI now returns to H and pushes to STU asynchronously

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
  DB -->> H --: committed
end

H -> NOTI ++: NotifyAsync(RefundSubmitted, userId, refundRequest.Id, { refundAmount })
NOTI -->> H --: queued
NOTI ->> STU: SignalR + FCM "refund request submitted"
H -->> CTRL --: Success(SubmitRefundRequestResponse)
CTRL -->> STU --: 201 Created (Location: /api/refunds/{id})
deactivate STU

note over H, DB: the AnyAsync check is not the race guard — the unique index is.\nDbUpdateException -> Rollback + "This order already has a pending or approved refund request."
@enduml
```
