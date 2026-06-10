# Setting Module API Reference

Base URL: `/api/settings`

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

## `GET /api/settings`
**Auth:** Manager

**Request** (query parameters):

| Parameter    | Type   | Required | Default | Description                  |
|-------------|--------|----------|---------|------------------------------|
| pageNumber  | int    | No       | 1       | Page number (min 1).         |
| pageSize    | int    | No       | 10      | Items per page (1-100).      |
| code        | string | No       | null    | Filter by code (LIKE).       |
| name        | string | No       | null    | Filter by name (LIKE).       |
| group       | string | No       | null    | Filter by group (LIKE).      |
| scope       | string | No       | null    | Filter by scope (LIKE).      |
| type        | string | No       | null    | Filter by type (LIKE).       |

**Validation Rules:**
- `pageNumber`: minimum 1 (clamped to 1 if below)
- `pageSize`: minimum 1, maximum 100 (clamped; default 10)

**Response 200:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "message": "Settings retrieved successfully.",
  "value": {
    "items": [
      {
        "id": "guid",
        "code": "string",
        "name": "string",
        "description": "string",
        "group": "string",
        "scope": "string",
        "value": "string",
        "type": "string"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 100,
    "totalPages": 10,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

**Handler Logic:**
1. Query all settings where `IsDeleted == false`.
2. For each non-null/whitespace filter parameter (`Code`, `Name`, `Group`, `Scope`, `Type`), apply a case-insensitive `Contains` filter on the corresponding field.
3. Order results by `Group` ASC, then `Scope` ASC, then `Code` ASC.
4. Apply pagination using `pageNumber` and `pageSize`.
5. Return `PaginatedList<GetAllSettingsResponse>`.

---

## `GET /api/settings/{id}`
**Auth:** Manager

**Request:** Route parameter only.

| Parameter | Type | Required | Description           |
|-----------|------|----------|-----------------------|
| id        | guid | Yes      | Setting unique identifier. |

**Response 200:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "message": "Setting retrieved successfully.",
  "value": {
    "id": "guid",
    "code": "string",
    "name": "string",
    "description": "string",
    "group": "string",
    "scope": "string",
    "value": "string",
    "type": "string"
  }
}
```

**Response 404:**

```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "NullValue",
    "message": "Value cannot be null.",
    "httpStatusCode": 400
  },
  "message": "Setting not found.",
  "value": null
}
```

**Handler Logic:**
1. Fetch setting by `Id`.
2. If `null` or `IsDeleted == true`, return failure with `Error.NullValue` and message `"Setting not found."`.
3. Return `GetSettingByIdResponse`.

---

## `POST /api/settings`
**Auth:** Manager

**Request** (JSON body):

```json
{
  "code": "string",
  "name": "string",
  "description": "string | null",
  "group": "string",
  "scope": "string",
  "value": "string",
  "type": "string"
}
```

**Validation Rules:**

| Field       | Rules |
|-------------|-------|
| code        | Required, max 255 chars, must match `^[A-Za-z0-9_.-]+$`. |
| name        | Required, max 255 chars. |
| description | Optional, max 500 chars. |
| group       | Required, max 100 chars. |
| scope       | Required, max 100 chars, must match `^[A-Za-z0-9_.-]+$`. |
| type        | Required, max 50 chars, one of: `string`, `int`, `decimal`, `bool`, `json`, `datetime`. |
| value       | Required, must be parseable according to `type`: <br>– `string`: any non-empty value<br>– `int`: valid `System.Int32`<br>– `decimal`: valid `System.Decimal`<br>– `bool`: valid `System.Boolean`<br>– `json`: valid JSON document<br>– `datetime`: valid `System.DateTimeOffset` |

**Response 201:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "message": "Setting created successfully.",
  "value": {
    "id": "guid",
    "code": "string",
    "name": "string",
    "description": "string",
    "group": "string",
    "scope": "string",
    "value": "string",
    "type": "string"
  }
}
```

**Response 400 (duplicate):**

```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "InvalidValue",
    "message": "Value is invalid.",
    "httpStatusCode": 400
  },
  "message": "Setting group, scope, and code already exist.",
  "value": null
}
```

**Response 403 (refund policy group):**

```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Forbidden",
    "message": "You do not have permission to perform this action.",
    "httpStatusCode": 403
  },
  "message": "Refund policy settings must be managed through the refund policy API.",
  "value": null
}
```

**Handler Logic:**
1. Normalize `Code`, `Group`, and `Scope` by trimming whitespace.
2. If `Group` equals `"REFUND_POLICY"` (case-insensitive), return `Error.Forbidden`.
3. Check uniqueness: query for an existing non-deleted setting with the same `Group`, `Scope`, and `Code` (case-insensitive). If found, return failure with `Error.InvalidValue`.
4. Create the aggregate root via `SettingAggregateRoot.Create(...)` with current user's `UserId`.
5. Begin a database transaction, persist, commit, then return the created setting.

---

## `PUT /api/settings/{id}`
**Auth:** Manager

**Request:** Route parameter + JSON body.

| Parameter | Type | Required | Source | Description |
|-----------|------|----------|--------|-------------|
| id        | guid | Yes      | Route  | Setting unique identifier. |
| name      | string | Yes    | Body   | |
| description | string \| null | No | Body | |
| group     | string | Yes    | Body   | |
| scope     | string | Yes    | Body   | |
| value     | string | Yes    | Body   | |
| type      | string | Yes    | Body   | |

**Note:** `id` is read from the route URL, not from the body (`[JsonIgnore]` on the command).

**Body:**

```json
{
  "name": "string",
  "description": "string | null",
  "group": "string",
  "scope": "string",
  "value": "string",
  "type": "string"
}
```

**Validation Rules:**

| Field       | Rules |
|-------------|-------|
| name        | Required, max 255 chars. |
| description | Optional, max 500 chars. |
| group       | Required, max 100 chars. |
| scope       | Required, max 100 chars, must match `^[A-Za-z0-9_.-]+$`. |
| type        | Required, max 50 chars, one of: `string`, `int`, `decimal`, `bool`, `json`, `datetime`. |
| value       | Required, must be parseable according to `type` (same rules as Create). |

**Response 200:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "message": "Setting updated successfully.",
  "value": {
    "id": "guid",
    "code": "string",
    "name": "string",
    "description": "string",
    "group": "string",
    "scope": "string",
    "value": "string",
    "type": "string"
  }
}
```

**Response 404:**

```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "NullValue",
    "message": "Value cannot be null.",
    "httpStatusCode": 400
  },
  "message": "Setting not found.",
  "value": null
}
```

**Response 403 (refund policy group):**

```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Forbidden",
    "message": "You do not have permission to perform this action.",
    "httpStatusCode": 403
  },
  "message": "Refund policy settings must be managed through the refund policy API.",
  "value": null
}
```

**Handler Logic:**
1. Fetch setting by `Id`. If `null` or `IsDeleted == true`, return `Error.NullValue`.
2. If the existing setting's `Group` **or** the request's `Group` equals `"REFUND_POLICY"` (case-insensitive), return `Error.Forbidden`.
3. Call `setting.Update(...)` with trimmed values and current user's `UserId` (in-place domain update).
4. Begin a database transaction, update, save, commit, then return the updated setting.
5. `Code` is **not** updatable — it is not present in the command model.

---

## `DELETE /api/settings/{id}`
**Auth:** Manager

**Request:** Route parameter only.

| Parameter | Type | Required | Description           |
|-----------|------|----------|-----------------------|
| id        | guid | Yes      | Setting unique identifier. |

**Response 200:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "message": "Setting deleted successfully.",
  "value": {
    "id": "guid",
    "message": "Setting deleted successfully."
  }
}
```

**Response 404:**

```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "NullValue",
    "message": "Value cannot be null.",
    "httpStatusCode": 400
  },
  "message": "Setting not found.",
  "value": null
}
```

**Response 403 (refund policy group):**

```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Forbidden",
    "message": "You do not have permission to perform this action.",
    "httpStatusCode": 403
  },
  "message": "Refund policy settings must be managed through the refund policy API.",
  "value": null
}
```

**Handler Logic:**
1. Fetch setting by `Id`. If `null` or `IsDeleted == true`, return `Error.NullValue`.
2. If the setting's `Group` equals `"REFUND_POLICY"` (case-insensitive), return `Error.Forbidden`.
3. Perform a soft delete by calling `setting.SoftDelete(currentUserService.UserId)`.
4. Begin a database transaction, update, save, commit, then return `DeleteSettingResponse` with `Id` and success message.

---

## Shared Models

### `PaginatedList<T>`

| Property        | Type       | Description                         |
|----------------|------------|-------------------------------------|
| items          | List\<T\>   | The items on the current page.      |
| pageNumber     | int        | Current page number.                |
| pageSize       | int        | Number of items per page.           |
| totalCount     | int        | Total number of items across all pages. |
| totalPages     | int        | Total number of pages.              |
| hasPreviousPage | bool      | `true` if `pageNumber > 1`.         |
| hasNextPage    | bool       | `true` if `pageNumber < totalPages`. |

### Setting Response (all endpoints except Delete)

| Property    | Type   | Nullable | Description                        |
|------------|--------|----------|------------------------------------|
| id         | guid   | No       | Setting unique identifier.         |
| code       | string | No       | Machine-readable key (unique within group+scope). |
| name       | string | No       | Human-readable display name.       |
| description| string | No       | Description (empty string if not provided). |
| group      | string | No       | Logical grouping category.         |
| scope      | string | No       | Scope within the group.            |
| value      | string | No       | The setting value (string-serialized). |
| type       | string | No       | Data type hint: `string`, `int`, `decimal`, `bool`, `json`, `datetime`. |

### `DeleteSettingResponse`

| Property | Type   | Nullable | Description                      |
|----------|--------|----------|----------------------------------|
| id       | guid   | No       | The deleted setting's identifier.|
| message  | string | No       | Success confirmation message.    |

---

## Error Codes Specific to This Module

| Error Code    | HTTP Status | Typical Cause                                  |
|---------------|-------------|------------------------------------------------|
| NullValue     | 400         | Setting not found (by id).                     |
| InvalidValue  | 400         | Duplicate (group + scope + code) on create.    |
| Forbidden     | 403         | Attempted CUD on a `REFUND_POLICY` setting.    |
| ServerError   | 500         | Unexpected exception during handler execution. |
