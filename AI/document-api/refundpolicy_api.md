# Refund Policy API Documentation

**Base URL (User):** `/api/refund-policies`  
**Base URL (Manager):** `/api/manager/refund-policies`  
**API Version:** `1.0`

---

## Common Response Envelope

Every endpoint returns a `Result<T>` wrapper:

```json
{
  "message": "string | null",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": { /* T payload */ }
}
```

On failure:

```json
{
  "message": "string | null",
  "isSuccess": false,
  "isFailure": true,
  "error": null,
  "value": null
}
```

> `Error` fields are decorated with `[JsonIgnore]` and **not serialized** in the response body. The HTTP status code is determined by the controller action (`400 BadRequest`, `404 NotFound`, `500` is caught and wrapped by handler).

---

## `GET /api/refund-policies`
**Auth:** Required (any authenticated user)  
**Summary:** Retrieve all active (complete & valid) refund policies.

### Response 200

```json
{
  "message": "Refund policies retrieved successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": [
    {
      "code": "string",
      "name": "string",
      "description": "string",
      "percent": 0.0,
      "requiresImage": true
    }
  ]
}
```

### Handler Logic (`GetActiveRefundPoliciesQueryHandler`)

1. Query all `IsDeleted == false` settings where `Group == "REFUND_POLICY"`.
2. Group settings by `Scope`.
3. For each scope group, attempt to build a `RefundPolicyDefinition` via `RefundPolicyDefinition.TryCreate()`:
   - Requires all four settings to exist (non-empty `NAME`, `DESCRIPTION`, parseable `PERCENT` > 0 and <= 100, parseable `REQUIRES_IMAGE` bool).
   - If any scope group is incomplete/invalid, it is **skipped** (logged as a warning).
4. Remaining valid policies are projected into `GetActiveRefundPoliciesResponse` (Code, Name, Description, Percent, RequiresImage).
5. Results are sorted alphabetically by `Name`.
6. On exception: log error, return `Result.Failure(Error.ServerError)`.

---

## `GET /api/manager/refund-policies`
**Auth:** Manager role required  
**Summary:** Retrieve all active refund policies (same logic as the user endpoint).

### Response 200

```json
{
  "message": "Refund policies retrieved successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": [
    {
      "code": "string",
      "name": "string",
      "description": "string",
      "percent": 0.0,
      "requiresImage": true
    }
  ]
}
```

### Handler Logic

Identical to `GET /api/refund-policies` — delegates to the same `GetActiveRefundPoliciesQueryHandler`.

---

## `POST /api/manager/refund-policies`
**Auth:** Manager role required  
**Summary:** Create a new refund policy.

### Request

```json
{
  "code": "string",
  "name": "string",
  "description": "string",
  "percent": 0.0,
  "requiresImage": true
}
```

### Validation Rules (`CreateRefundPolicyCommandValidator`)

| Field          | Rule                     |
|----------------|--------------------------|
| `code`         | Required, max 100 chars, must match `^[A-Za-z0-9_.-]+$` |
| `name`         | Required, max 255 chars  |
| `description`  | Max 500 chars            |
| `percent`      | Must be > 0 and <= 100   |

### Response 201

```json
{
  "message": "Refund policy created successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": {
    "code": "string",
    "name": "string",
    "description": "string",
    "percent": 0.0,
    "requiresImage": true
  }
}
```

### Handler Logic (`CreateRefundPolicyCommandHandler`)

1. Normalize `request.Code` → `scope = code.Trim().ToUpperInvariant()`.
2. Check if a non-deleted policy with the same `Group == "REFUND_POLICY"` and `Scope == scope` already exists.
   - If yes → return `Result.Failure(Error.InvalidValue, "Refund policy code already exists.")`.
3. Create four `Setting` entities (`NAME`, `DESCRIPTION`, `PERCENT`, `REQUIRES_IMAGE`) under the `"REFUND_POLICY"` group and the computed `scope`.
4. Begin a database transaction.
5. Insert all four settings via `settingRepository.AddAsync()`.
6. Commit the transaction.
7. On `DbUpdateException` (e.g. unique constraint): rollback, log warning, return `Result.Failure(Error.InvalidValue, "Refund policy code already exists.")`.
8. On any other exception: rollback, log error, return `Result.Failure(Error.ServerError)`.

---

## `PUT /api/manager/refund-policies/{code}`
**Auth:** Manager role required  
**Summary:** Update an existing refund policy (all four settings are overwritten).

### Request — Path Parameters

| Parameter | Type   | Required | Description                     |
|-----------|--------|----------|---------------------------------|
| `code`    | string | Yes      | Refund policy scope identifier  |

### Request — Body

```json
{
  "name": "string",
  "description": "string",
  "percent": 0.0,
  "requiresImage": true
}
```

> `code` in the body is **overridden** by the route parameter before sending to the handler.

### Validation Rules (`UpdateRefundPolicyCommandValidator`)

| Field          | Rule                     |
|----------------|--------------------------|
| `code`         | Required, max 100 chars, must match `^[A-Za-z0-9_.-]+$` |
| `name`         | Required, max 255 chars  |
| `description`  | Max 500 chars            |
| `percent`      | Must be > 0 and <= 100   |

### Response 200

```json
{
  "message": "Refund policy updated successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": {
    "code": "string",
    "name": "string",
    "description": "string",
    "percent": 0.0,
    "requiresImage": true
  }
}
```

### Handler Logic (`UpdateRefundPolicyCommandHandler`)

1. Normalize `request.Code` → `scope = code.Trim().ToUpperInvariant()`.
2. Fetch all non-deleted settings for `Group == "REFUND_POLICY"` and `Scope == scope`.
   - If none found → return `Result.Failure(Error.NullValue, "Refund policy not found.")`.
3. Validate the existing settings can form a complete policy via `RefundPolicyDefinition.TryCreate()`.
   - If not → return `Result.Failure(Error.InvalidValue, "Refund policy data is incomplete or invalid.")`.
4. Build a dictionary of new values for the four setting codes.
5. Begin a database transaction.
6. Iterate over the existing settings; for each setting whose `Code` matches one of `NAME`, `DESCRIPTION`, `PERCENT`, `REQUIRES_IMAGE`, call `setting.Update()` with the new value.
7. Commit the transaction.
8. On exception: rollback, log error, return `Result.Failure(Error.ServerError)`.

---

## `DELETE /api/manager/refund-policies/{code}`
**Auth:** Manager role required  
**Summary:** Soft-delete an existing refund policy.

### Request — Path Parameters

| Parameter | Type   | Required | Description                     |
|-----------|--------|----------|---------------------------------|
| `code`    | string | Yes      | Refund policy scope identifier  |

### Response 200

```json
{
  "message": "Refund policy deleted successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": "string (scope)"
}
```

### Handler Logic (`DeleteRefundPolicyCommandHandler`)

1. Normalize `request.Code` → `scope = code.Trim().ToUpperInvariant()`.
2. Fetch all non-deleted settings for `Group == "REFUND_POLICY"` and `Scope == scope`.
   - If none found → return `Result.Failure(Error.NullValue, "Refund policy not found.")`.
3. Begin a database transaction.
4. For each setting, call `setting.SoftDelete(currentUserService.UserId)` and mark it as updated.
5. Commit the transaction.
6. On exception: rollback, log error, return `Result.Failure(Error.ServerError)`.
