# Update Spec — User Category in Auth APIs

**Audience:** Frontend  
**Date:** 2026-08-15  
**Scope:** `POST /api/auth/register`, `POST /api/auth/verify-email`, `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/google`, `GET /api/auth/me`, `PUT /api/auth/me`

---

## 1. What changed in one paragraph

Registration now requires the caller to declare a **user category**. A **Student** keeps the current flow: register → receive a 6-digit code by email → `verify-email` → login. A **Lecturer** skips the code entirely: the account is created with the email already confirmed, so the FE must send them straight to login without showing the code screen. **Staff** and **External** are rejected — those accounts are provisioned by a manager. Several auth responses now return the category so the FE can branch on it without an extra call.

---

## 2. `UserCategory` enum

| Value | Name | Self sign-up | Email code required |
| ----- | -------- | ------------ | ------------------- |
| `1` | Student | ✅ allowed | ✅ yes |
| `2` | Lecturer | ✅ allowed | ❌ no |
| `3` | Staff | ❌ rejected | — |
| `4` | External | ❌ rejected | — |

Sent and received as an **integer**, same convention as `role`, `status`, and `gender`.

---

## 3. `POST /api/auth/register`

### 3.1 Request — new required field `category`

```json
{
  "name": "Nguyen Van A",
  "email": "an@fpt.edu.vn",
  "password": "Str0ng@Pass",
  "category": 1,
  "studentId": "SE170001",
  "dateOfBirth": "2004-01-15",
  "majorOrClass": "SE1701",
  "phoneNumber": "0912345678",
  "address": "Hoa Lac",
  "gender": 1
}
```

`category` is **required**. Everything after it is optional and unchanged. Omitting `category` is a validation error (it binds to `0`, which is not a known value).

### 3.2 Response — `201 Created`

Student:
```json
{
  "value": {
    "userId": "3f7c...guid",
    "category": 1,
    "requiresEmailVerification": true
  },
  "isSuccess": true,
  "message": "Registration successful. Please verify your email."
}
```

Lecturer:
```json
{
  "value": {
    "userId": "3f7c...guid",
    "category": 2,
    "requiresEmailVerification": false
  },
  "isSuccess": true,
  "message": "Registration successful. You can sign in now."
}
```

> **Branch on `value.requiresEmailVerification`, not on the category integer.** It is the single flag that decides whether the code screen appears; if the rule ever widens to another category the FE keeps working unchanged.

### 3.3 Errors

| HTTP | Shape | `errorCode` | When |
| ---- | ----- | ----------- | ---- |
| `400` | Result envelope | `UnsupportedUserCategory` | `category` is `3` (Staff), `4` (External), or any value outside the enum, and the request reached the handler |
| `400` | `ValidationProblemDetails` | — | FluentValidation rejected the body before the handler ran |
| `409` | Result envelope | `EmailAlreadyExists` | email taken |
| `409` | Result envelope | `StudentIdAlreadyUsed` | student ID taken |

Rejected-category result envelope:
```json
{
  "isSuccess": false,
  "errorCode": "UnsupportedUserCategory",
  "message": "Registration is only available for Student and Lecturer accounts."
}
```

Validation-problem shape (auto-validation, fires first in practice):
```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Category": ["Registration is only available for Student and Lecturer accounts."]
  }
}
```
The other possible message on the `Category` key is `"User category is not a known value."` (value outside `1..4`, including a missing field).

> Handle **both** shapes on this endpoint — the FE already does this elsewhere, but the category rule is enforced in two layers, so both can surface.

---

## 4. Flow the FE must implement

### 4.1 Student (`category: 1`)

1. `POST /api/auth/register` → `201`, `requiresEmailVerification: true`.
2. Show the "enter the code we emailed you" screen.
3. `POST /api/auth/verify-email` with `{ "email", "code" }`.
4. On success, go to login. Logging in before this step fails with `401 EmailNotVerified`.

### 4.2 Lecturer (`category: 2`)

1. `POST /api/auth/register` → `201`, `requiresEmailVerification: false`.
2. **Skip the code screen** — no email is sent, and calling `verify-email` would fail with `InvalidOrExpiredToken` because no code exists.
3. Go straight to login; it succeeds immediately.
4. Account status after registering:
   - FPT mailbox (`@fpt.edu.vn` / `@fe.edu.vn`) → `Active` (`status: 1`), full access.
   - Any other mailbox → `PendingIdentityVerification` (`status: 3`). Login works, but the account is not fully verified — the existing identity-verification prompt/flow applies exactly as it does today for non-FPT students after they verify their email.

### 4.3 Staff / External (`category: 3` / `4`)

Do not offer these in the sign-up form. If sent anyway, expect the `400` described above.

---

## 5. Responses that now include `category`

### 5.1 `AuthTokensDto` — `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/google`

```json
{
  "value": {
    "accessToken": "string",
    "accessTokenExpiresAt": "2026-08-15T10:00:00+00:00",
    "refreshToken": "string",
    "refreshTokenExpiresAt": "2026-08-22T10:00:00+00:00",
    "category": 1
  },
  "isSuccess": true,
  "message": "Login successful."
}
```

The category is available at login time, so category-dependent UI does not have to wait for `GET /api/auth/me`. It is also refreshed on every token rotation.

### 5.2 `GET /api/auth/me` and `PUT /api/auth/me`

`category` is added to the profile object, right after `role`:

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

`category` is **read-only**. `PUT /api/auth/me` echoes it but cannot change it — same as `role`, `status`, `email`, `studentId`, and `balance`.

---

## 6. Google sign-in

`POST /api/auth/google` is unchanged for the caller — no new request field. The backend derives the category from the mailbox for accounts it creates: `@fe.edu.vn` → Lecturer, any other FPT address → Student. The value comes back in the `category` field of the token response.

---

## 7. Existing accounts

The migration adds the column with a default of `Student` and backfills every existing `@fe.edu.vn` account to `Lecturer`. No account is left without a category, so `category` is always present in responses — never `null`.

---

## 8. FE checklist

- [ ] Add a category selector (Student / Lecturer only) to the sign-up form; send it as `category`.
- [ ] Treat `category` as required client-side — the request fails without it.
- [ ] Route post-registration on `value.requiresEmailVerification`: `true` → code screen, `false` → login.
- [ ] Handle `UnsupportedUserCategory` (result envelope) and the `Category` key inside `ValidationProblemDetails`.
- [ ] Add `category` to the auth/session store from the login, refresh, and Google responses.
- [ ] Add `category` to the user/profile model consumed from `GET /api/auth/me` and `PUT /api/auth/me`.
- [ ] Keep the existing identity-verification prompt for `status: 3` — lecturers on non-FPT mailboxes land there too.
- [ ] Show `category` in the manager user list/detail, and add a category filter to the list screen.

---

## 9. Manager user endpoints

`GET /api/manager/users` and `GET /api/manager/users/{id}` now return `category` (right after `role`), and the list accepts an optional `category` query parameter (`1..4`) alongside `status` and `role`. Like `role`, the category is **read-only** here — no manager endpoint changes it.

---

## 10. Not part of this change

- The **access token claims** are unchanged: they still carry user id, email, role, and the verified flag. There is no `category` claim — read it from the response bodies above.
- `studentId` is still **optional** for Student registrations; the backend does not require it.
