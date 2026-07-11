# SmartCanteen Verification API

Base URL: `/api/verification` (user) / `/api/admin/verifications` (manager)  
API Version: `1.0`

DocumentType: `1=StudentCard, 2=NationalId, 3=Other`  
VerificationStatus: `1=Pending, 2=Approved, 3=Rejected, 4=Expired`

---

## User Endpoints

### `POST /api/verification/submit`
Auth: `[Authorize]`  
Content-Type: `multipart/form-data`

**Fields:**
- `files`: `List<IFormFile>` — at least 1 required
- `documentTypes`: `List<int>` — one per file, must match count

**Validation:**
- Account status must be `PendingIdentityVerification` (3)
- Only one pending request at a time
- Files validated by `IFileValidator` (name, size, MIME)
- Uploaded to Cloudinary

**201:**
```json
{ "value": "guid", "isSuccess": true, "message": "Verification request submitted." }
```

**Errors:** `400` — no files, count mismatch, account not awaiting verification, pending request exists.

---

### `GET /api/verification/me`
Auth: `[Authorize]`

```json
{
  "value": {
    "requestId": "guid | null",
    "status": 0,
    "submittedAt": "... | null",
    "reviewedAt": "... | null",
    "rejectionReason": "string | null",
    "hasOpenRequest": true
  },
  "isSuccess": true
}
```

---

## Manager Endpoints

Auth: `[Authorize(Roles = "Manager")]` for all.

### `GET /api/admin/verifications`
**Query:** `?pageNumber=1&pageSize=10&status=1`

- `status` optional, defaults to `1=Pending`
- VerificationStatus: `1=Pending, 2=Approved, 3=Rejected, 4=Expired`

Paginated items:
```json
{
  "id": "guid",
  "userId": "guid",
  "userEmail": "string",
  "userName": "string",
  "status": 1,
  "submittedAt": "...",
  "documentCount": 0
}
```

### `GET /api/admin/verifications/{id}`
```json
{
  "value": {
    "id": "guid",
    "userId": "guid",
    "userEmail": "string",
    "userName": "string",
    "studentId": "string | null",
    "majorOrClass": "string | null",
    "dateOfBirth": "... | null",
    "status": 0,
    "submittedAt": "...",
    "reviewedAt": "... | null",
    "reviewedBy": "guid | null",
    "rejectionReason": "string | null",
    "expiresAt": "...",
    "documents": [
      {
        "id": "guid",
        "documentType": 0,
        "cloudinaryUrl": "string",
        "fileName": "string",
        "fileSize": 0,
        "mimeType": "string",
        "uploadedAt": "..."
      }
    ]
  },
  "isSuccess": true
}
```
`404` if not found.

### `POST /api/admin/verifications/{id}/approve`
No body. Returns:
```json
{ "value": { "id": "guid", "message": "string" }, "isSuccess": true }
```

### `POST /api/admin/verifications/{id}/reject`
```json
{ "reason": "string" }
```
Returns:
```json
{ "value": { "id": "guid", "message": "string" }, "isSuccess": true }
```
