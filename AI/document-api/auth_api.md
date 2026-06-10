# Auth Module API Reference

Base URL: `/api/auth`

All endpoints return a uniform response envelope (`Result<TValue>`):

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "message": "Human-readable status message",
  "value": { }
}
```

On failure:

```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "ErrorCode",
    "message": "Error description",
    "httpStatusCode": 400
  },
  "message": "Additional context",
  "value": null
}
```

---

## `POST /api/auth/register`
**Auth:** AllowAnonymous

**Request** (JSON body):

```json
{
  "name": "string (required, 2-200 chars)",
  "email": "string (required, max 256, valid email format)",
  "password": "string (required, 8-100 chars, must contain uppercase, lowercase, digit, special char)",
  "studentId": "string | null (optional, max 50)",
  "dateOfBirth": "string (ISO date) | null (optional, user must be 10-100 years old)",
  "majorOrClass": "string | null (optional, max 200)",
  "phoneNumber": "string | null (optional, max 20, must match Vietnamese phone format: 0xxx or +84xxx)",
  "address": "string | null (optional, max 500)",
  "gender": "number (int) | null (optional, 1=Male, 2=Female, 3=Other)"
}
```

**Validation Rules:**

| Field | Rules |
|---|---|
| `name` | Required. Min 2 chars. Max 200 chars. |
| `email` | Required. Max 256 chars. Must pass `UserAggregate.IsValidEmail()` format check. Normalized to lowercase trim. |
| `password` | Required. Min 8 chars. Max 100 chars. Must contain ≥1 uppercase, ≥1 lowercase, ≥1 digit, ≥1 special character (`!@#$%^&*()_\-+=\[\]{}|;:'"<>,.?/~\``). |
| `gender` | If provided, must be a valid `Gender` enum value (1, 2, or 3). |
| `dateOfBirth` | If provided, user must be at least 10 years old and no older than 100 years. |
| `studentId` | If provided (non-null/whitespace), max 50 chars. Also checked for uniqueness against existing accounts. |
| `phoneNumber` | If provided (non-null/whitespace), max 20 chars. Must match `^(0\|\+84)(3\|5\|7\|8\|9)\d{8}$`. |
| `majorOrClass` | If provided, max 200 chars. |
| `address` | If provided, max 500 chars. |

**Response 201 Created:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "message": "Registration successful. Please verify your email.",
  "value": {
    "userId": "guid"
  }
}
```

**Error Codes:**

| Code | HTTP | Condition |
|---|---|---|
| `EmailAlreadyExists` | 409 | Email is already in use by another account. |
| `StudentIdAlreadyUsed` | 409 | Student ID is already linked to another account. |
| `ServerError` | 500 | Unexpected server error; account creation rolled back. |

**Handler Logic:**
1. Normalizes email (trim, lowercase).
2. Checks email uniqueness. If taken, returns `EmailAlreadyExists`.
3. If `studentId` is provided, checks student ID uniqueness. If taken, returns `StudentIdAlreadyUsed`.
4. Hashes password via `IPasswordHasher`.
5. Calls `UserAggregate.Register(...)` to create domain entity.
6. Opens DB transaction, persists user.
7. Generates opaque email verification token with configurable TTL (default 24h, from `Jwt:EmailVerificationHours`).
8. Issues `EmailVerificationToken` linked to user ID.
9. Persists token, commits transaction.
10. Best-effort sends verification email via `IEmailSender`. If sending fails, logs error but does NOT roll back registration.
11. Returns `201 Created` with user ID.

---

## `GET /api/auth/verify-email?token={token}`
**Auth:** AllowAnonymous

**Request** (query parameter):

| Param | Type | Required | Description |
|---|---|---|---|
| `token` | string | Yes | The raw opaque verification token sent via email. |

**Validation Rules:**

| Field | Rules |
|---|---|
| `token` | Required. Must not be empty. |

**Response 200 OK:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "message": "Email verified successfully.",
  "value": {
    "message": "Email verified successfully."
  }
}
```

**Error Codes:**

| Code | HTTP | Condition |
|---|---|---|
| `InvalidOrExpiredToken` | 400 | Token hash not found in DB, token expired, already consumed, or linked user no longer exists. |
| `ServerError` | 500 | Unexpected server error. |

**Handler Logic:**
1. Hashes the raw token using `IJwtTokenGenerator.HashOpaqueToken()`.
2. Looks up `EmailVerificationToken` by hash.
3. If token is null or `IsValid` is false (expired/consumed), returns `InvalidOrExpiredToken`.
4. Fetches linked user by `token.UserId`.
5. If user not found, returns `InvalidOrExpiredToken`.
6. Calls `user.ConfirmEmail()` (marks email as verified, sets status to `Active`).
7. Calls `token.Consume()` (marks token as used).
8. Opens transaction, updates both entities, commits.
9. Returns success response.

---

## `POST /api/auth/login`
**Auth:** AllowAnonymous

**Request** (JSON body):

```json
{
  "email": "string (required, max 256, valid email format)",
  "password": "string (required, max 100)"
}
```

**Validation Rules:**

| Field | Rules |
|---|---|
| `email` | Required. Max 256 chars. Must pass `UserAggregate.IsValidEmail()` format check. |
| `password` | Required. Max 100 chars. |

**Response 200 OK:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "message": "Login successful.",
  "value": {
    "accessToken": "string (JWT)",
    "accessTokenExpiresAt": "datetime (ISO 8601)",
    "refreshToken": "string (opaque token)",
    "refreshTokenExpiresAt": "datetime (ISO 8601)"
  }
}
```

**Error Codes:**

| Code | HTTP | Condition |
|---|---|---|
| `InvalidCredentials` | 401 | Email not found, password mismatch, or account uses Google sign-in (no password set). |
| `EmailNotVerified` | 401 | User's email is not yet verified. |
| `AccountSuspended` | 401 | Account status is `Suspended` or `Banned`. |
| `ServerError` | 500 | Unexpected server error. |

**Handler Logic:**
1. Normalizes email (trim, lowercase).
2. Looks up user by email. If not found, returns `InvalidCredentials`.
3. If `user.PasswordHash` is null (Google-only account), returns `InvalidCredentials` with "continue with Google" message.
4. Verifies password against hash via `IPasswordHasher.Verify()`. Fails → `InvalidCredentials`.
5. If `!user.EmailVerified`, returns `EmailNotVerified`.
6. If user status is `Suspended` or `Banned`, returns `AccountSuspended`.
7. Generates access token (JWT) with user ID, email, role, and verification status.
8. Generates opaque refresh token with configurable TTL (default 7 days, from `Jwt:RefreshTokenDays`).
9. Calls `user.RecordLogin()` to update `LastLoginAt`.
10. Opens transaction, persists refresh token, updates user, commits.
11. Returns tokens pair.

---

## `POST /api/auth/refresh`
**Auth:** AllowAnonymous

**Request** (JSON body):

```json
{
  "refreshToken": "string (required, the opaque refresh token)"
}
```

**Validation Rules:**

| Field | Rules |
|---|---|
| `refreshToken` | Required. Must not be empty. |

**Response 200 OK:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "message": "Token refreshed.",
  "value": {
    "accessToken": "string (new JWT)",
    "accessTokenExpiresAt": "datetime (ISO 8601)",
    "refreshToken": "string (new opaque token, rotation)",
    "refreshTokenExpiresAt": "datetime (ISO 8601)"
  }
}
```

**Error Codes:**

| Code | HTTP | Condition |
|---|---|---|
| `InvalidRefreshToken` | 401 | Token hash not found, expired, revoked, or linked account is suspended/banned. |
| `ServerError` | 500 | Unexpected server error. |

**Handler Logic:**
1. Hashes the raw refresh token.
2. Looks up `RefreshToken` by hash.
3. If not found or `IsActive` is false (expired/revoked), returns `InvalidRefreshToken`.
4. Opens transaction.
5. Fetches linked user. If user is null or status is `Suspended`/`Banned`, revokes the old token (rotation kill), persists, and returns `InvalidRefreshToken`.
6. Generates **new** access token + **new** opaque refresh token.
7. Rotates: calls `existing.Revoke(newRefresh.Id)` (revokes old token, stores replacement chain).
8. Persists new refresh token, updates existing (revoked) token, commits.
9. Returns new token pair.

---

## `POST /api/auth/google`
**Auth:** AllowAnonymous

**Request** (JSON body):

```json
{
  "idToken": "string (required, Google Identity Services JWT)"
}
```

**Validation Rules:**

| Field | Rules |
|---|---|
| `idToken` | Required. Must not be empty. |

**Response 200 OK:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "message": "Google sign-in successful.",
  "value": {
    "accessToken": "string (JWT)",
    "accessTokenExpiresAt": "datetime (ISO 8601)",
    "refreshToken": "string (opaque token)",
    "refreshTokenExpiresAt": "datetime (ISO 8601)"
  }
}
```

**Error Codes:**

| Code | HTTP | Condition |
|---|---|---|
| `GoogleTokenInvalid` | 401 | Google ID token is invalid, expired, or malformed. |
| `GoogleEmailNotVerified` | 401 | The Google account's email is not verified with Google. |
| `NonFptGoogleAccount` | 403 | Email domain is not an FPT University account. |
| `AccountSuspended` | 401 | Existing account status is `Suspended` or `Banned`. |
| `ServerError` | 500 | Unexpected server error. |

**Handler Logic:**
1. Validates Google ID token via `IGoogleTokenValidator.ValidateAsync()`. If null, returns `GoogleTokenInvalid`.
2. If Google-reported `EmailVerified` is false, returns `GoogleEmailNotVerified`.
3. Normalizes email (trim, lowercase). Checks `UserAggregate.IsFptEmail()` — only FPT University emails allowed. Fails → `NonFptGoogleAccount`.
4. Looks up user by email:
   - **New user:** Creates user via `UserAggregate.RegisterWithGoogle()`. Attempts to extract student ID from email (e.g., student code in email prefix). If student ID is already taken by another account, stores null instead (BR-07 hard constraint).
   - **Existing user:** Checks account status. If `Suspended`/`Banned`, returns `AccountSuspended`. If first Google sign-in (`user.GoogleSubjectId is null`), links Google identity via `user.LinkGoogle()`.
5. Calls `user.RecordLogin()`.
6. Opens transaction, persists user (add or update), generates and persists refresh token, commits.
7. Generates access token, returns token pair.

---

## `POST /api/auth/logout`
**Auth:** Authorize (valid JWT required)

**Request** (JSON body):

```json
{
  "refreshToken": "string (required, the opaque refresh token to revoke)"
}
```

**Validation Rules:**

| Field | Rules |
|---|---|
| `refreshToken` | Required. Must not be empty. |

**Response 200 OK:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "message": "Logged out.",
  "value": {
    "message": "Logged out."
  }
}
```

**Error Codes:**

| Code | HTTP | Condition |
|---|---|---|
| `ServerError` | 500 | Unexpected server error. |

**Handler Logic:**
1. Hashes the raw refresh token.
2. Looks up `RefreshToken` by hash.
3. If not found, returns success immediately (idempotent — already logged out).
4. Opens transaction, calls `token.Revoke()`, updates, commits.
5. Returns success response.

---

## `GET /api/auth/me`
**Auth:** Authorize (valid JWT required)

**Request:** No body. The authenticated user is resolved from the JWT claims via `ICurrentUserService`.

**Response 200 OK:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "message": "Profile retrieved.",
  "value": {
    "id": "guid",
    "name": "string",
    "email": "string",
    "imgUrl": "string | null",
    "role": 3,
    "status": 2,
    "emailVerified": true,
    "studentId": "string | null",
    "dateOfBirth": "YYYY-MM-DD | null",
    "majorOrClass": "string | null",
    "phoneNumber": "string | null",
    "address": "string | null",
    "gender": 1 | 2 | 3 | null,
    "balanceAmount": "decimal",
    "lastLoginAt": "datetime (ISO 8601) | null"
  }
}
```

**Error Codes:**

| Code | HTTP | Condition |
|---|---|---|
| `Forbidden` | 403 | User not authenticated (empty `UserId` from JWT) or user not found in DB. |
| `ServerError` | 500 | Unexpected server error. |

**Handler Logic:**
1. Extracts `UserId` from JWT via `ICurrentUserService.UserId`.
2. If `Guid.Empty`, returns `Forbidden` ("Not authenticated").
3. Fetches user by ID from repository.
4. If not found, returns `Forbidden` ("User not found").
5. Maps all user fields to `UserProfileResponse` and returns.

---

## Enums Reference

### `Gender`

| Value | Name |
|---|---|
| 1 | Male |
| 2 | Female |
| 3 | Other |

### `Role`

| Value | Name |
|---|---|
| 1 | Admin |
| 2 | Manager |
| 3 | User |
| 4 | Staff |

### `AccountStatus`

| Value | Name |
|---|---|
| 1 | Active |
| 2 | PendingEmailVerification |
| 3 | PendingIdentityVerification |
| 4 | Suspended |
| 5 | Banned |

---

## Shared Response Models

### `AuthTokensDto`

| Property | Type | Description |
|---|---|---|
| `accessToken` | string | JWT access token (short-lived). |
| `accessTokenExpiresAt` | datetime (ISO 8601) | Access token expiry. |
| `refreshToken` | string | Opaque refresh token (for token rotation). |
| `refreshTokenExpiresAt` | datetime (ISO 8601) | Refresh token expiry. |

### `UserProfileResponse`

| Property | Type | Nullable | Description |
|---|---|---|---|
| `id` | guid | no | User's unique identifier. |
| `name` | string | no | Display name. |
| `email` | string | no | Email address. |
| `imgUrl` | string | yes | Profile image URL. |
| `role` | int (enum) | no | `Role` enum value. |
| `status` | int (enum) | no | `AccountStatus` enum value. |
| `emailVerified` | bool | no | Whether email is confirmed. |
| `studentId` | string | yes | Institutional student ID. |
| `dateOfBirth` | date string | yes | Date of birth (ISO 8601 date). |
| `majorOrClass` | string | yes | Major or class name. |
| `phoneNumber` | string | yes | Phone number. |
| `address` | string | yes | Physical address. |
| `gender` | int (enum) | yes | `Gender` enum value (1-3). |
| `balanceAmount` | decimal | no | Wallet balance. |
| `lastLoginAt` | datetime (ISO 8601) | yes | Last login timestamp. |

---

## Error Reference (Auth-scoped)

| Code | HTTP | Description |
|---|---|---|
| `EmailAlreadyExists` | 409 | Registration: email taken. |
| `StudentIdAlreadyUsed` | 409 | Registration: student ID taken. |
| `InvalidCredentials` | 401 | Login: wrong email/password or Google-only account. |
| `EmailNotVerified` | 401 | Login: email not yet verified. |
| `AccountSuspended` | 401 | Account status is Suspended or Banned. |
| `InvalidOrExpiredToken` | 400 | Verify-email: token invalid/expired/consumed. |
| `InvalidRefreshToken` | 401 | Refresh: token invalid/revoked/expired or user banned. |
| `Forbidden` | 403 | `/me`: no valid authentication. |
| `GoogleTokenInvalid` | 401 | Google: ID token invalid/expired. |
| `GoogleEmailNotVerified` | 401 | Google: email not verified by Google. |
| `NonFptGoogleAccount` | 403 | Google: non-FPT email domain. |
| `ServerError` | 500 | Unexpected internal error. |
