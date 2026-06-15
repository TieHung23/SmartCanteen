# SmartCanteen User API

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

## Auth

### `POST /api/auth/register`
**Auth:** AllowAnonymous  

**Request body:**
```json
{
  "name": "string",
  "email": "string",
  "password": "string",
  "studentId": "string | null",
  "dateOfBirth": "2024-01-15 | null",
  "majorOrClass": "string | null",
  "phoneNumber": "string | null",
  "address": "string | null",
  "gender": 1 | null
}
```
Gender: `1=Male, 2=Female, 3=Other`

**201 Response:**
```json
{
  "value": {
    "userId": "guid"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `GET /api/auth/verify-email`
**Auth:** AllowAnonymous  
**Query:** `?token=string`

**Response:**
```json
{
  "value": {
    "message": "string"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/auth/login`
**Auth:** AllowAnonymous  

**Request body:**
```json
{
  "email": "string",
  "password": "string"
}
```

**Response:**
```json
{
  "value": {
    "accessToken": "string",
    "accessTokenExpiresAt": "2024-01-01T00:00:00Z",
    "refreshToken": "string",
    "refreshTokenExpiresAt": "2024-01-01T00:00:00Z"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/auth/google`
**Auth:** AllowAnonymous  

**Request body:**
```json
{
  "idToken": "string"
}
```

**Response:** Same `AuthTokensDto` as login.

### `POST /api/auth/refresh`
**Auth:** AllowAnonymous  

**Request body:**
```json
{
  "refreshToken": "string"
}
```

**Response:** Same `AuthTokensDto` as login.

### `POST /api/auth/logout`
**Auth:** Authorize  

**Request body:**
```json
{
  "refreshToken": "string"
}
```

**Response:**
```json
{
  "value": {
    "message": "string"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `GET /api/auth/me`
**Auth:** Authorize  

**Response:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "email": "string",
    "imgUrl": "string | null",
    "role": 3,
    "status": 1,
    "emailVerified": true,
    "studentId": "string | null",
    "dateOfBirth": "2024-01-15 | null",
    "majorOrClass": "string | null",
    "phoneNumber": "string | null",
    "address": "string | null",
    "gender": 1 | null,
    "balanceAmount": 0.0,
    "lastLoginAt": "2024-01-01T00:00:00Z | null"
  },
  "isSuccess": true,
  "message": "string"
}
```
Role: `1=Admin, 2=Manager, 3=User, 4=Staff`  
AccountStatus: `1=Active, 2=PendingEmailVerification, 3=PendingIdentityVerification, 4=Suspended, 5=Banned`

### `PUT /api/auth/me`
**Auth:** Authorize

**Content-Type:** `multipart/form-data`

**Form fields:**
- `name` (required)
- `dateOfBirth` (optional)
- `majorOrClass` (optional)
- `phoneNumber` (optional)
- `address` (optional)
- `gender` (optional)
- `image` (optional avatar) — JPEG or PNG, maximum 5 MB

When `image` is omitted, the existing avatar is preserved. Clients cannot update
`imgUrl` directly; uploaded avatars are stored in Cloudinary.

Gender: `1=Male, 2=Female, 3=Other`

**Validation Rules:**
- `name`: Required, 2–200 characters
- `dateOfBirth`: Must indicate age ≥ 10 and ≤ 100 years
- `majorOrClass`: Optional, max 200 chars
- `phoneNumber`: Optional, max 20 chars, must match Vietnamese format (e.g. `0912345678` or `+84912345678`)
- `address`: Optional, max 500 chars
- `gender`: Optional, `1=Male, 2=Female, 3=Other`

Optional string fields are cleared when sent as `null` or an empty string. Student ID,
email, role, account status, email verification status and balance cannot be changed by
this endpoint.

**Response:** Same profile shape as `GET /api/auth/me`.

---

