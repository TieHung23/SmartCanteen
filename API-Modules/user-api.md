# SmartCanteen User / Auth API

Base URL: `/api/auth`  
API Version: `1.0`

Auth notes:
- Endpoints marked `AllowAnonymous` do not require a token.
- Endpoints marked `Authorize` require a valid JWT Bearer token.
- `PUT /api/auth/me` uses `multipart/form-data` (other endpoints use JSON body).
- Banned/suspended accounts are blocked by middleware on every authorized request, including when the access token was issued before the status changed.
- When an account is banned or suspended through the manager user endpoints, all active refresh tokens are revoked.
- Failure responses include `errorCode` for FE branching, for example `AccountBanned`, `AccountSuspended`, `UserNotFound`.

---

## `POST /api/auth/register`
AllowAnonymous. Creates a new user account. Students are sent an email verification code; lecturers are not.

```json
{
  "name": "string",
  "email": "string",
  "password": "string",
  "category": 1,
  "studentId": "string | null",
  "dateOfBirth": "2024-01-15 | null",
  "majorOrClass": "string | null",
  "phoneNumber": "string | null",
  "address": "string | null",
  "gender": 1
}
```
Gender: `1=Male, 2=Female, 3=Other`  
UserCategory: `1=Student, 2=Lecturer, 3=Staff, 4=External` — self sign-up accepts `Student` and `Lecturer` only.

**Category behaviour:**
- `1` Student — account is created as `PendingEmailVerification`; a code is emailed and `POST /api/auth/verify-email` must be called before login.
- `2` Lecturer — the email is confirmed on creation, so no code is sent and no verification call is needed. The account becomes `Active` for FPT mailboxes, or `PendingIdentityVerification` otherwise.
- `3` Staff / `4` External — rejected; these accounts are provisioned by a manager.

**201:**
```json
{
  "value": { "userId": "guid", "category": 1, "requiresEmailVerification": true },
  "isSuccess": true,
  "message": "Registration successful. Please verify your email."
}
```
For a lecturer, `requiresEmailVerification` is `false` and the message is `"Registration successful. You can sign in now."`

**Errors:**
- `400` `UnsupportedUserCategory` — category is Staff, External, or not a known value.
- `400` — email taken, student ID taken, validation failure.

---

## `POST /api/auth/verify-email`
AllowAnonymous. Student accounts only — lecturer accounts are created already verified.

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
    "refreshTokenExpiresAt": "...",
    "category": 1
  },
  "isSuccess": true
}
```
UserCategory: `1=Student, 2=Lecturer, 3=Staff, 4=External`

**Errors:**
- `401` `EmailNotVerified` — email is not verified.
- `401` `AccountSuspended` — account is suspended.
- `403` `AccountBanned` — account is banned.
- `400/401` — invalid credentials or Google-only account.

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

**Errors:**
- `401` — invalid/expired refresh token.
- `401` `AccountSuspended` — account is suspended; provided refresh token is revoked.
- `403` `AccountBanned` — account is banned; provided refresh token is revoked.

---

## `POST /api/auth/google`
AllowAnonymous. Only FPT University emails accepted.

```json
{ "idToken": "string" }
```

**200:** Same `AuthTokensDto` shape as login.

**Errors:**
- `401` — invalid Google token.
- `403` — non-FPT email.
- `401` `AccountSuspended` — account is suspended.
- `403` `AccountBanned` — account is banned.

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
    "category": 1,
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
UserCategory: `1=Student, 2=Lecturer, 3=Staff, 4=External`  
AccountStatus: `1=Active, 2=PendingEmailVerification, 3=PendingIdentityVerification, 4=Suspended, 5=Banned`

**Authorized request blocked by account status middleware:**

`403 AccountSuspended`
```json
{
  "isSuccess": false,
  "errorCode": "AccountSuspended",
  "message": "This account has been suspended.",
  "reason": "Violation reason | null"
}
```

`403 AccountBanned`
```json
{
  "isSuccess": false,
  "errorCode": "AccountBanned",
  "message": "This account has been banned.",
  "reason": "Violation reason | null"
}
```

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

---

# SmartCanteen Manager User API

Base URL: `/api/manager/users`  
API Version: `1.0`

Auth notes:
- Requires JWT Bearer token.
- Requires `Manager` role.
- Manager cannot change their own account status through these endpoints.
- `Suspend` and `Ban` both revoke all active refresh tokens for the target user.
- `Reactivate` sets a suspended user back to `Active`. Banned users cannot be reactivated.
- Existing access tokens for the target user are blocked on the next authorized request by account status middleware.
- Suspend/ban stores the current lock reason in `Users.StatusReason`.
- Reactivate clears `Users.StatusReason`.
- `category` (`1=Student, 2=Lecturer, 3=Staff, 4=External`) is returned on the list and detail endpoints and is read-only — no manager endpoint changes it.

---

## `GET /api/manager/users`
Authorize: `Manager`.

**Query:**
- `pageNumber` default `1`
- `pageSize` default `10`, max `100`
- `search` optional, matches name/email/student ID
- `status` optional, `1=Active, 2=PendingEmailVerification, 3=PendingIdentityVerification, 4=Suspended, 5=Banned`
- `role` optional, `1=Admin, 2=Manager, 3=User, 4=Staff`
- `category` optional, `1=Student, 2=Lecturer, 3=Staff, 4=External`

**200:**
```json
{
  "value": {
    "items": [
      {
        "id": "guid",
        "name": "string",
        "email": "string",
        "imgUrl": "string | null",
        "role": 3,
        "category": 1,
        "status": 1,
        "statusReason": "string | null",
        "emailVerified": true,
        "studentId": "string | null",
        "majorOrClass": "string | null",
        "phoneNumber": "string | null",
        "balanceAmount": 0.0,
        "lastLoginAt": "... | null",
        "createdAtUtc": "..."
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 0,
    "totalPages": 0,
    "hasPreviousPage": false,
    "hasNextPage": false
  },
  "isSuccess": true,
  "message": "Users retrieved."
}
```

---

## `GET /api/manager/users/{id}`
Authorize: `Manager`.

**200:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "email": "string",
    "imgUrl": "string | null",
    "role": 3,
    "category": 1,
    "status": 1,
    "statusReason": "string | null",
    "emailVerified": true,
    "studentId": "string | null",
    "dateOfBirth": "2024-01-15 | null",
    "majorOrClass": "string | null",
    "phoneNumber": "string | null",
    "address": "string | null",
    "gender": 1,
    "balanceAmount": 0.0,
    "lastLoginAt": "... | null",
    "createdAtUtc": "...",
    "updatedAtUtc": "... | null"
  },
  "isSuccess": true,
  "message": "User detail retrieved."
}
```

**Errors:**
- `404` `UserNotFound` — user not found.

---

## `POST /api/manager/users/{id}/suspend`
Authorize: `Manager`.

```json
{ "reason": "string" }
```

**200:**
```json
{
  "value": {
    "userId": "guid",
    "status": 4,
    "revokedRefreshTokenCount": 2,
    "reason": "Violation reason",
    "message": "User account has been suspended."
  },
  "isSuccess": true,
  "message": "User account has been suspended."
}
```

**Errors:**
- `400` — invalid user id, missing reason, or manager attempts to suspend their own account.
- `404` `UserNotFound` — user not found.

Notes:
- Stores `reason` in `Users.StatusReason`.
- Revokes all active refresh tokens of the target user.
- Any existing access token is blocked on the next authorized request by account status middleware.

---

## `POST /api/manager/users/{id}/ban`
Authorize: `Manager`.

```json
{ "reason": "string" }
```

**200:**
```json
{
  "value": {
    "userId": "guid",
    "status": 5,
    "revokedRefreshTokenCount": 2,
    "reason": "Violation reason",
    "message": "User account has been banned."
  },
  "isSuccess": true,
  "message": "User account has been banned."
}
```

**Errors:**
- `400` — invalid user id, missing reason, or manager attempts to ban their own account.
- `404` `UserNotFound` — user not found.

Notes:
- Stores `reason` in `Users.StatusReason`.
- Revokes all active refresh tokens of the target user.
- Any existing access token is blocked on the next authorized request by account status middleware.

---

## `POST /api/manager/users/{id}/reactivate`
Authorize: `Manager`.

Reactivates a suspended account by setting `status` to `Active`. Banned accounts cannot be reactivated.

**200:**
```json
{
  "value": {
    "userId": "guid",
    "status": 1,
    "message": "User account has been reactivated."
  },
  "isSuccess": true,
  "message": "User account has been reactivated."
}
```

**Errors:**
- `400` — invalid user id, manager attempts to reactivate their own account, account is not suspended, or account is banned.
- `404` `UserNotFound` — user not found.

Notes:
- Clears `Users.StatusReason`.
