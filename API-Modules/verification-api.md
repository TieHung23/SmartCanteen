# SmartCanteen Verification API

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

