# Verification Module — API Reference

- **Base URL:** `/api/verification` (user-facing) · `/api/admin/verifications` (admin)
- **Auth:** All endpoints require a valid JWT Bearer token (`Authorization: Bearer <token>`).
- **Version:** `1.0`
- **Content-Type:** `application/json` (except submit which is `multipart/form-data`)
- **Common Error Envelope:**

```json
{
  "isSuccess": false,
  "error": { "code": "ErrorCode", "message": "Human-readable message." },
  "value": null
}
```

---

## User-Facing Endpoints

---

## `POST /api/verification/submit`

**Auth:** Authenticated user (`AccountStatus.PendingIdentityVerification`)

**Request:** `multipart/form-data`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `files` | `IFormFile[]` | Yes | At least one file. Accepted MIME types: `image/jpeg`, `image/png`, `application/pdf`. Max size: 5 MB per file. |
| `documentTypes` | `int[]` | Yes | Must have the same number of elements as `files`. Each value maps to a `DocumentType`: `1` = StudentCard, `2` = NationalId, `3` = Other. |

```json
// Form payload — sent as multipart/form-data, not raw JSON
{
  "files": [<binary_file>, <binary_file>],
  "documentTypes": [1, 2]
}
```

**Validation Rules:**

| Field | Rule |
|-------|------|
| `files` | Non-null; at least 1 file; count must equal `documentTypes` count |
| `files[i].FileName` | Not empty |
| `files[i].FileSize` | > 0; ≤ 5,242,880 bytes (5 MB) |
| `files[i].MimeType` | Must be one of: `image/jpeg`, `image/png`, `application/pdf` |
| `documentTypes` | Non-null; count must equal `files` count |
| Account status | User must have `AccountStatus.PendingIdentityVerification` |
| Duplicate request | Only one `Pending` verification request allowed at a time (BR-39) |

**Response 201 Created:**

```json
{
  "isSuccess": true,
  "error": null,
  "value": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

The `value` is the `Guid` of the newly created `VerificationRequest`.

**Handler Logic:**

1. Extract `UserId` from the JWT via `ICurrentUserService`.
2. Fetch the `User` — must exist and have `Status == PendingIdentityVerification`.
3. Check no existing `Pending` verification exists for this user (BR-39).
4. Validate every file against the `IFileValidator` (name, size, MIME type) **before** uploading any (BR-30/BR-31 — avoids orphan files).
5. Upload each file via `IFileUploader` (Cloudinary). If any upload fails, the entire request fails.
6. Create `VerificationDocument` value objects for each uploaded file.
7. Call `VerificationRequest.Submit(userId, documents, ttl)` — sets status to `Pending`, `SubmittedAt = UtcNow`, `ExpiresAt = UtcNow + 14 days` (configurable via `Verification:RequestExpiryDays`).
8. Persist inside a transaction. On failure, rollback.

**Error Codes:**

| Code | HTTP | Condition |
|------|------|-----------|
| `Forbidden` | 403 | Not authenticated or user not found |
| `AccountNotActive` | 403 | Account is not in `PendingIdentityVerification` state |
| `VerificationAlreadyPending` | 409 | User already has a pending request |
| `EmptyValue` | 400 | No files provided or file name empty / file empty |
| `FileTooLarge` | 413 | File exceeds 5 MB |
| `UnsupportedFileFormat` | 400 | MIME type not in `image/jpeg`, `image/png`, `application/pdf` |
| `InvalidValue` | 400 | `documentTypes` count does not match `files` count |
| `ServerError` | 500 | File upload failed or unexpected server error |

---

## `GET /api/verification/me`

**Auth:** Authenticated user

**Request:** No body / no query parameters.

**Response 200:**

```json
{
  "isSuccess": true,
  "error": null,
  "value": {
    "requestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": 1,
    "submittedAt": "2026-06-10T12:00:00Z",
    "reviewedAt": null,
    "rejectionReason": null,
    "hasOpenRequest": true
  }
}
```

**Response 200 — No request found:**

```json
{
  "isSuccess": true,
  "error": null,
  "value": {
    "requestId": null,
    "status": null,
    "submittedAt": null,
    "reviewedAt": null,
    "rejectionReason": null,
    "hasOpenRequest": false
  }
}
```

**VerificationStatus enum values:**

| Value | Name |
|-------|------|
| 1 | Pending |
| 2 | Approved |
| 3 | Rejected |
| 4 | Expired |

**Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `requestId` | `guid?` | ID of the latest request, or `null` |
| `status` | `int?` | `VerificationStatus` integer, or `null` |
| `submittedAt` | `datetime?` | When the request was submitted |
| `reviewedAt` | `datetime?` | When an admin reviewed it (null if pending) |
| `rejectionReason` | `string?` | Admin's rejection reason (null if not rejected) |
| `hasOpenRequest` | `bool` | `true` if the latest request is still `Pending` |

**Handler Logic:**

1. Extract `UserId` from JWT.
2. Query `VerificationRequest` table for the latest record (`OrderByDescending(SubmittedAt)`) for this user.
3. If no record exists, return `null` fields with `hasOpenRequest = false`.
4. Otherwise map to `VerificationStatusResponse` and return.

**Error Codes:**

| Code | HTTP | Condition |
|------|------|-----------|
| `Forbidden` | 403 | Not authenticated |
| `ServerError` | 500 | Unexpected server error |

---

## Admin Endpoints

---

## `GET /api/admin/verifications`

**Auth:** `Admin` role

**Request:** Query string

| Query | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `pageNumber` | `int` | No | `1` | Page number (clamped to ≥ 1) |
| `pageSize` | `int` | No | `10` | Items per page (clamped 1–100) |

**Response 200:**

```json
{
  "isSuccess": true,
  "error": null,
  "value": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "userEmail": "student@fpt.edu.vn",
        "userName": "Nguyen Van A",
        "submittedAt": "2026-06-10T12:00:00Z",
        "documentCount": 2
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 25,
    "totalPages": 3,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

**Response 200 — Empty list:**

```json
{
  "isSuccess": true,
  "error": null,
  "value": {
    "items": [],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 0,
    "totalPages": 0,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

**Handler Logic:**

1. Query `VerificationRequest` filtered by `Status == Pending`.
2. Apply pagination: order by `SubmittedAt` ascending, skip `(pageNumber - 1) * pageSize`, take `pageSize`.
3. Get distinct `UserId` values and batch-fetch users from the `User` repository.
4. Project to `PendingVerificationItem` (Id, UserId, UserEmail, UserName, SubmittedAt, DocumentCount).

**Error Codes:**

| Code | HTTP | Condition |
|------|------|-----------|
| `ServerError` | 500 | Unexpected server error |

---

## `GET /api/admin/verifications/{id}`

**Auth:** `Admin` role

**Request:** Path parameter

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | `guid` | Yes | Verification request ID |

**Response 200:**

```json
{
  "isSuccess": true,
  "error": null,
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userEmail": "student@fpt.edu.vn",
    "userName": "Nguyen Van A",
    "studentId": "SE123456",
    "majorOrClass": "SE1234",
    "dateOfBirth": "2000-01-15",
    "status": 1,
    "submittedAt": "2026-06-10T12:00:00Z",
    "reviewedAt": null,
    "reviewedBy": null,
    "rejectionReason": null,
    "expiresAt": "2026-06-24T12:00:00Z",
    "documents": [
      {
        "id": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
        "documentType": 1,
        "cloudinaryUrl": "https://res.cloudinary.com/.../image.jpg",
        "fileName": "student_card.jpg",
        "fileSize": 245760,
        "mimeType": "image/jpeg",
        "uploadedAt": "2026-06-10T12:00:00Z"
      }
    ]
  }
}
```

**DocumentType enum values:**

| Value | Name |
|-------|------|
| 1 | StudentCard |
| 2 | NationalId |
| 3 | Other |

**Handler Logic:**

1. Query `VerificationRequest` by `Id`.
2. If not found → `Error.VerificationNotFound` (404).
3. Fetch the associated `User` by `UserId`.
4. Map documents to `VerificationDocumentDto`.
5. Return full detail including user profile data (StudentId, MajorOrClass, DateOfBirth).

**Error Codes:**

| Code | HTTP | Condition |
|------|------|-----------|
| `VerificationNotFound` | 404 | Verification request ID not found |
| `ServerError` | 500 | Unexpected server error |

---

## `POST /api/admin/verifications/{id}/approve`

**Auth:** `Admin` role

**Request:** Path parameter only — no body.

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | `guid` | Yes | Verification request ID to approve |

**Response 200:**

```json
{
  "isSuccess": true,
  "error": null,
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "message": "Verification approved."
  }
}
```

**Handler Logic:**

1. Fetch `VerificationRequest` by `Id` — must exist and have `Status == Pending`.
2. Fetch the associated `User`.
3. Call `verification.Approve(reviewerId)` → sets `Status = Approved`, `ReviewedAt = UtcNow`, `ReviewedBy = reviewerId`.
4. Call `user.ActivateAfterIdentityApproved()` → transitions account to `Active`.
5. Persist both entities in a transaction.
6. Send approval email via `IEmailSender.SendVerificationStatusAsync(user.Email, approved: true, ...)`. Email failure is logged but does **not** fail the operation.

**Error Codes:**

| Code | HTTP | Condition |
|------|------|-----------|
| `VerificationNotFound` | 404 | Verification request not found |
| `VerificationNotPending` | 409 | Request is already approved/rejected/expired |
| `InvalidValue` | 400 | Domain invariant violation (e.g. `EnsurePending()` fails) |
| `ServerError` | 500 | Unexpected server error |

---

## `POST /api/admin/verifications/{id}/reject`

**Auth:** `Admin` role

**Request:** Path parameter + JSON body

| Param | Type | Location | Required | Description |
|-------|------|----------|----------|-------------|
| `id` | `guid` | Path | Yes | Verification request ID |
| `Reason` | `string` | Body | Yes | Rejection reason |

**Request body:**

```json
{
  "reason": "Student card is blurry and does not show full name."
}
```

**Validation Rules:**

| Field | Rule |
|-------|------|
| `reason` | Required; minimum 10 characters; maximum 500 characters |

**Response 200:**

```json
{
  "isSuccess": true,
  "error": null,
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "message": "Verification rejected."
  }
}
```

**Handler Logic:**

1. Validate `Reason` is not null/whitespace.
2. Fetch `VerificationRequest` by `Id` — must exist and have `Status == Pending`.
3. Call `verification.Reject(reviewerId, reason)` → sets `Status = Rejected`, `ReviewedAt = UtcNow`, `ReviewedBy = reviewerId`, `RejectionReason = reason`.
4. Persist in a transaction.
5. Fetch the associated `User` and send rejection email via `IEmailSender.SendVerificationStatusAsync(user.Email, approved: false, rejectionReason: reason, ...)`. Email failure is logged but does **not** fail the operation.

**Error Codes:**

| Code | HTTP | Condition |
|------|------|-----------|
| `RejectionReasonRequired` | 400 | `reason` is null, empty, or whitespace |
| `VerificationNotFound` | 404 | Verification request not found |
| `VerificationNotPending` | 409 | Request is already approved/rejected/expired |
| `InvalidValue` | 400 | Reason < 10 characters or > 500 characters (FluentValidation) |
| `ServerError` | 500 | Unexpected server error |

---

## Domain Model Reference

### VerificationStatus

| Value | Name | Description |
|-------|------|-------------|
| 1 | Pending | Awaiting admin review |
| 2 | Approved | Documents accepted, user activated |
| 3 | Rejected | Documents rejected, reason provided |
| 4 | Expired | Request TTL elapsed without review |

### DocumentType

| Value | Name | Description |
|-------|------|-------------|
| 1 | StudentCard | University student ID card |
| 2 | NationalId | Government-issued ID / Citizen ID |
| 3 | Other | Any other supporting document |

### AccountStatus (used by verification flow)

| Value | Name |
|-------|------|
| 3 | PendingIdentityVerification — required state to submit |
| 1 | Active — set after approval |

### VerificationRequest (aggregate root)

| Property | Type | Notes |
|----------|------|-------|
| `Id` | `Guid` | Primary key |
| `UserId` | `Guid` | FK to User |
| `Status` | `VerificationStatus` | Current state |
| `SubmittedAt` | `DateTimeOffset` | Creation timestamp |
| `ReviewedAt` | `DateTimeOffset?` | When admin acted |
| `ReviewedBy` | `Guid?` | Admin user who reviewed |
| `RejectionReason` | `string?` | Only set when rejected |
| `ExpiresAt` | `DateTimeOffset` | Auto-expiry (`SubmittedAt + 14d`) |
| `Documents` | `IReadOnlyCollection<VerificationDocument>` | Uploaded files |

### VerificationDocument (value object)

| Property | Type | Notes |
|----------|------|-------|
| `Id` | `Guid` | |
| `VerificationRequestId` | `Guid` | FK back to parent |
| `DocumentType` | `DocumentType` | Category of document |
| `CloudinaryUrl` | `string` | Upload destination URL |
| `FileName` | `string` | Original filename |
| `FileSize` | `long` | Size in bytes |
| `MimeType` | `string` | e.g. `image/jpeg` |
| `UploadedAt` | `DateTimeOffset` | Upload timestamp |

---

## Configuration (appsettings.json section)

```json
{
  "Verification": {
    "FileSizeMaxBytes": 5242880,
    "AcceptedFormats": ["image/jpeg", "image/png", "application/pdf"],
    "EmailTokenHours": 24,
    "RequestExpiryDays": 14
  }
}
```
