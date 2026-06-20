# SmartCanteen User / Auth API

Base URL: `/api/auth`  
API Version: `1.0`

Auth notes:
- Endpoints marked `AllowAnonymous` do not require a token.
- Endpoints marked `Authorize` require a valid JWT Bearer token.
- `PUT /api/auth/me` uses `multipart/form-data` (other endpoints use JSON body).

---

## `POST /api/auth/register`
AllowAnonymous. Creates a new user account. Sends email verification code.

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
  "gender": 1
}
```
Gender: `1=Male, 2=Female, 3=Other`

**201:**
```json
{ "value": { "userId": "guid" }, "isSuccess": true, "message": "Registration successful. Please verify your email." }
```

**Errors:** `400` — email taken, student ID taken, validation failure.

---

## `POST /api/auth/verify-email`
AllowAnonymous.

```json
{ "email": "string", "code": "string" }
```

**200:**
```json
{ "value": { "message": "Email verified successfully." }, "isSuccess": true }
```

**Errors:** `400` — invalid/expired token.

---

## `POST /api/auth/login`
AllowAnonymous.

```json
{ "email": "string", "password": "string" }
```

**200:**
```json
{
  "value": {
    "accessToken": "string",
    "accessTokenExpiresAt": "...",
    "refreshToken": "string",
    "refreshTokenExpiresAt": "..."
  },
  "isSuccess": true
}
```

**Errors:** `400` — invalid credentials, email not verified, account suspended, Google-only account.

---

## `POST /api/auth/forgot-password`
AllowAnonymous. Always returns success to prevent email enumeration.

```json
{ "email": "string" }
```

**200:**
```json
{ "value": { "message": "If an eligible account exists for this email, a password reset link has been sent." }, "isSuccess": true }
```

---

## `POST /api/auth/reset-password`
AllowAnonymous.

```json
{ "token": "string", "newPassword": "string", "confirmPassword": "string" }
```

**200:**
```json
{ "value": { "message": "Password reset successfully. Please sign in again." }, "isSuccess": true }
```

**Errors:** `400` — invalid/expired token.

---

## `PUT /api/auth/change-password`
Authorize.

```json
{ "currentPassword": "string", "newPassword": "string", "confirmPassword": "string" }
```

**200:**
```json
{ "value": { "message": "Password changed successfully. Please sign in again." }, "isSuccess": true }
```

**Errors:** `400` — incorrect current password, Google-only account.

---

## `POST /api/auth/refresh`
AllowAnonymous. Token rotation — the old refresh token is revoked.

```json
{ "refreshToken": "string" }
```

**200:** Same `AuthTokensDto` shape as login.

**Errors:** `400` — invalid/expired refresh token.

---

## `POST /api/auth/google`
AllowAnonymous. Only FPT University emails accepted.

```json
{ "idToken": "string" }
```

**200:** Same `AuthTokensDto` shape as login.

**Errors:** `400` — invalid Google token, non-FPT email, account suspended.

---

## `POST /api/auth/logout`
Authorize.

```json
{ "refreshToken": "string" }
```

**200:**
```json
{ "value": { "message": "Logged out." }, "isSuccess": true }
```

---

## `GET /api/auth/me`
Authorize.

**200:**
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
    "gender": 1,
    "balanceAmount": 0.0,
    "lastLoginAt": "... | null"
  },
  "isSuccess": true
}
```
Role: `1=Admin, 2=Manager, 3=User, 4=Staff`  
AccountStatus: `1=Active, 2=PendingEmailVerification, 3=PendingIdentityVerification, 4=Suspended, 5=Banned`

---

## `PUT /api/auth/me`
Authorize. `Content-Type: multipart/form-data`

**Fields:**
- `name` (required, 2–200 chars)
- `dateOfBirth` (optional, age ≥ 10 and ≤ 100)
- `majorOrClass` (optional, max 200 chars)
- `phoneNumber` (optional, max 20 chars, Vietnamese format e.g. `0912345678`)
- `address` (optional, max 500 chars)
- `gender` (optional, `1=Male, 2=Female, 3=Other`)
- `image` (optional, avatar file, JPEG/PNG, max 5 MB) — uploaded to Cloudinary

Optional string fields are cleared when sent as `null` or empty. Student ID, email, role, status, and balance cannot be changed via this endpoint.

**200:** Same `UserProfileResponse` shape as `GET /api/auth/me`.
