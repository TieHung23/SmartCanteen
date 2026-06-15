# Category Module API Documentation

**Base URL:** `api/v1/Categories` (resolves to `api/Categories` for v1.0)

**Auth:** All endpoints require `[Authorize]` — a valid JWT bearer token must be provided via the `Authorization` header.

**Controller:** `SC.Api.Controllers.CategoriesController`

---

## Common Response Wrapper

Every endpoint returns a `Result<TValue>` (or `Result` for errors). All response bodies follow this shape:

### Success Response
```json
{
  "isSuccess": true,
  "isFailure": false,
  "message": "Descriptive success message.",
  "error": null,
  "value": { /* payload */ }
}
```

### Failure Response
```json
{
  "isSuccess": false,
  "isFailure": true,
  "message": "Descriptive error message.",
  "error": {
    "code": "ErrorCodeString",
    "message": "Human-readable error description.",
    "httpStatusCode": 400
  },
  "value": null
}
```

| Field       | Type          | Nullable | Description                              |
|-------------|---------------|----------|------------------------------------------|
| `isSuccess` | `boolean`     | No       | Indicates if the operation succeeded.    |
| `isFailure` | `boolean`     | No       | Inverse of `isSuccess`.                  |
| `message`   | `string`      | Yes      | Human-readable result message.           |
| `error`     | `Error`       | Yes      | Populated only when `isFailure == true`. |
| `value`     | `TValue / null` | Yes    | Populated only when `isSuccess == true`. |

### Error Object
| Field            | Type     | Nullable | Description                            |
|------------------|----------|----------|----------------------------------------|
| `code`           | `string` | No       | Machine-readable error code.           |
| `message`        | `string` | No       | Human-readable error description.      |
| `httpStatusCode` | `int`    | No       | Corresponding HTTP status code.        |

**Predefined errors observed in this module:**
- `Error.NullValue` → code: `"NullValue"`, status: `400` — entity not found
- `Error.ServerError` → code: `"ServerError"`, status: `500` — unexpected server error

---

## `GET /api/Categories`

**Auth:** Required

**Request — Query Parameters:**

| Parameter    | Type     | Required | Default | Max    | Description                                      |
|-------------|----------|----------|---------|--------|--------------------------------------------------|
| `pageNumber` | `int`    | No       | `1`     | —      | Page number (clamped to ≥1).                     |
| `pageSize`   | `int`    | No       | `10`    | `100`  | Items per page (clamped: <1→10, >100→100).       |
| `Name`       | `string` | No       | —       | —      | Filters categories whose name contains this value (case-insensitive). |

Full query string example:
```
?pageNumber=1&pageSize=10&Name=drink
```

**Validation:**
- `pageNumber`: clamped to minimum 1 (values < 1 become 1)
- `pageSize`: clamped to 1–100 range (values < 1 become 10, >100 become 100)
- `Name`: no validation rules — passed as-is; whitespace/empty strings skip the filter

**Response `200 OK` — success:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "message": "Categories retrieved successfully.",
  "error": null,
  "value": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "Beverages",
        "description": "All kinds of drinks",
        "imgUrl": "https://res.cloudinary.com/.../image.jpg"
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

| `value` Field       | Type                           | Nullable | Description                               |
|---------------------|--------------------------------|----------|-------------------------------------------|
| `items`             | `array<GetAllCategoriesResponse>` | No    | List of categories on the current page.   |
| `pageNumber`        | `int`                          | No       | Current page number.                      |
| `pageSize`          | `int`                          | No       | Items per page.                           |
| `totalCount`        | `int`                          | No       | Total number of non-deleted categories.   |
| `totalPages`        | `int`                          | No       | Total number of pages.                    |
| `hasPreviousPage`   | `boolean`                      | No       | `true` if `pageNumber > 1`.               |
| `hasNextPage`       | `boolean`                      | No       | `true` if `pageNumber < totalPages`.      |

**GetAllCategoriesResponse:**

| Field         | Type     | Nullable | Description                    |
|---------------|----------|----------|--------------------------------|
| `id`          | `guid`   | No       | Category unique identifier.    |
| `name`        | `string` | No       | Category name.                 |
| `description` | `string` | No       | Category description.          |
| `imgUrl`      | `string` | Yes      | Cloudinary image URL (if any). |

**Response `400 Bad Request` / `500 Internal Server Error`:**
Returns the common failure `Result` wrapper with the appropriate error.

---

**Handler Logic Summary:**

1. Builds a queryable from `IGenericRepository<Category>`.
2. Filters out soft-deleted categories (`IsDeleted == false`).
3. If `Name` query param is provided and non-whitespace, applies a case-insensitive `Contains` filter on category name.
4. Counts total matching records.
5. Applies pagination (skip/take) ordered by `Name` ascending.
6. Maps entities to `GetAllCategoriesResponse` DTOs.
7. Wraps in `PaginatedList<T>` and returns `Result.Success`.

---

## `GET /api/Categories/{id}`

**Auth:** Required

**Request — Route Parameters:**

| Parameter | Type   | Required | Description                    |
|-----------|--------|----------|--------------------------------|
| `id`      | `guid` | Yes      | Category unique identifier.    |

Route constraint: `{id:guid}` — must be a valid GUID.

**Response `200 OK` — success:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "message": "Category retrieved successfully.",
  "error": null,
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Beverages",
    "description": "All kinds of drinks",
    "imgUrl": "https://res.cloudinary.com/.../image.jpg",
    "createdAtUtc": "2026-06-10T12:00:00Z",
    "updatedAtUtc": "2026-06-10T14:30:00Z",
    "createdBy": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "updatedBy": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  }
}
```

| `value` Field    | Type           | Nullable | Description                                |
|------------------|----------------|----------|--------------------------------------------|
| `id`             | `guid`         | No       | Category unique identifier.                |
| `name`           | `string`       | No       | Category name.                             |
| `description`    | `string`       | No       | Category description.                      |
| `imgUrl`         | `string`       | Yes      | Cloudinary image URL (if any).             |
| `createdAtUtc`   | `datetimeoffset` | No     | UTC timestamp of creation.                 |
| `updatedAtUtc`   | `datetimeoffset` | Yes    | UTC timestamp of last update (null if never updated). |
| `createdBy`      | `guid`         | No       | ID of the user who created the category.   |
| `updatedBy`      | `guid`         | No       | ID of the user who last updated the category. |

**Response `404 Not Found` — category does not exist or is soft-deleted:**

```json
{
  "isSuccess": false,
  "isFailure": true,
  "message": "Category not found.",
  "error": {
    "code": "NullValue",
    "message": "Value cannot be null.",
    "httpStatusCode": 400
  },
  "value": null
}
```

**Response `500 Internal Server Error`:**
Common failure `Result` with `Error.ServerError`.

---

**Handler Logic Summary:**

1. Calls `categoryRepository.GetByIdAsync(id)`.
2. If the entity is `null` or `IsDeleted == true`, returns `Result.Failure` with `Error.NullValue`.
3. Maps the entity to `GetCategoryByIdResponse` (includes audit fields: `CreatedAtUtc`, `UpdatedAtUtc`, `CreatedBy`, `UpdatedBy`).
4. Returns `Result.Success`.

---

## `POST /api/Categories`

**Auth:** Required

**Request — Form Data (`multipart/form-data`):**

| Field         | Type          | Required | Description                                      |
|---------------|---------------|----------|--------------------------------------------------|
| `name`        | `string`      | Yes      | Category name. Trimmed before persistence.       |
| `description` | `string`      | Yes      | Category description. Trimmed before persistence.|
| `image`       | `IFormFile`   | No       | Optional image file (uploaded to Cloudinary).    |

**Validation:**
- No explicit FluentValidation rules in this module. The domain `Category.Create(...)` method may apply its own invariants.
- `name`: trimmed via `request.Name.Trim()` before use; an empty or whitespace-only string may be rejected by the domain aggregate root.
- `description`: trimmed via `request.Description.Trim()` before use.
- If `image` is provided, it is uploaded to Cloudinary. If upload fails, the request will fail.

**Response `201 Created` — success:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "message": "Category created successfully.",
  "error": null,
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Beverages",
    "description": "All kinds of drinks",
    "imgUrl": "https://res.cloudinary.com/.../image.jpg"
  }
}
```

| `value` Field | Type     | Nullable | Description                                  |
|---------------|----------|----------|----------------------------------------------|
| `id`          | `guid`   | No       | Newly created category ID.                   |
| `name`        | `string` | No       | Category name.                               |
| `description` | `string` | No       | Category description.                        |
| `imgUrl`      | `string` | Yes      | Cloudinary image URL (null if no image was uploaded). |

**Headers:**
- `Location`: `/api/v1/Categories/{id}` — URL to fetch the newly created category via `GET /api/Categories/{id}`.

**Response `400 Bad Request` — failure (e.g., domain validation error, Cloudinary failure):**

```json
{
  "isSuccess": false,
  "isFailure": true,
  "message": "An error occurred while creating the category.",
  "error": {
    "code": "ServerError",
    "message": "An unexpected server error occurred.",
    "httpStatusCode": 500
  },
  "value": null
}
```

**Response `500 Internal Server Error`:**
Transaction is rolled back; common failure `Result` with `Error.ServerError`.

---

**Handler Logic Summary:**

1. If an `image` file is provided, the controller uploads it to Cloudinary via `cloudinaryUpload.UploadFileAsync(stream, fileName)` and extracts `ViewUrl` as `imgUrl`.
2. Constructs `CreateCategoryCommand` with `Name`, `Description`, and optional `ImgUrl`.
3. Handler calls `CategoryAggregateRoot.Create(name.Trim(), description.Trim(), currentUserId)` — the domain factory creates the entity.
4. Assigns `imgUrl` to `category.ImgUrl`.
5. Begins a database transaction (`unitOfWork.BeginTransactionAsync`).
6. Adds the category to the repository, saves changes, commits the transaction.
7. Maps to `CreateCategoryResponse` and returns `Result.Success`.
8. On exception, rolls back the transaction and returns `Result.Failure` with `Error.ServerError`.

---

## `PUT /api/Categories/{id}`

**Auth:** Required

**Request — Route Parameters:**

| Parameter | Type   | Required | Description                    |
|-----------|--------|----------|--------------------------------|
| `id`      | `guid` | Yes      | Category unique identifier.    |

Route constraint: `{id:guid}` — must be a valid GUID.

**Request — Form Data (`multipart/form-data`):**

| Field         | Type          | Required | Description                                      |
|---------------|---------------|----------|--------------------------------------------------|
| `name`        | `string`      | Yes      | Category name. Trimmed before persistence.       |
| `description` | `string`      | Yes      | Category description. Trimmed before persistence.|
| `image`       | `IFormFile`   | No       | Optional image file (uploaded to Cloudinary). If not provided, the existing `ImgUrl` is preserved. |

Note: The `id` from the route is mapped to `UpdateCategoryCommand.Id` (decorated with `[JsonIgnore]` so it never comes from the body).

**Validation:**
- No explicit FluentValidation rules.
- `name` and `description` are trimmed before update.
- If `image` is not provided, the existing category's `ImgUrl` is retained (`request.ImgUrl ?? category.ImgUrl`).

**Response `200 OK` — success:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "message": "Category updated successfully.",
  "error": null,
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Beverages",
    "description": "All kinds of drinks",
    "imgUrl": "https://res.cloudinary.com/.../image.jpg"
  }
}
```

**Response `400 Bad Request` — category not found or not found (soft-deleted):**

```json
{
  "isSuccess": false,
  "isFailure": true,
  "message": "Category not found.",
  "error": {
    "code": "NullValue",
    "message": "Value cannot be null.",
    "httpStatusCode": 400
  },
  "value": null
}
```

**Response `500 Internal Server Error`:**
Transaction is rolled back; common failure `Result` with `Error.ServerError`.

---

**Handler Logic Summary:**

1. If an `image` file is provided, the controller uploads it to Cloudinary and extracts `ViewUrl` as the new `imgUrl`.
2. Constructs `UpdateCategoryCommand` with `Id` (from route), `Name`, `Description`, and optional `ImgUrl`.
3. Handler fetches the existing category by `Id` via `categoryRepository.GetByIdAsync`.
4. If entity is `null` or `IsDeleted == true`, returns `Result.Failure` with `Error.NullValue`.
5. Calls `category.Update(name.Trim(), description.Trim(), currentUserId, imgUrl ?? category.ImgUrl)` — domain method updates the entity.
6. Begins a database transaction, marks the entity as updated, saves changes, commits.
7. Maps to `UpdateCategoryResponse` and returns `Result.Success`.
8. On exception, rolls back the transaction and returns `Result.Failure` with `Error.ServerError`.

---

## `DELETE /api/Categories/{id}`

**Auth:** Required

**Request — Route Parameters:**

| Parameter | Type   | Required | Description                    |
|-----------|--------|----------|--------------------------------|
| `id`      | `guid` | Yes      | Category unique identifier.    |

Route constraint: `{id:guid}` — must be a valid GUID.

**Response `200 OK` — success:**

```json
{
  "isSuccess": true,
  "isFailure": false,
  "message": "Category deleted successfully.",
  "error": null,
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "message": "Category deleted successfully."
  }
}
```

| `value` Field | Type     | Nullable | Description                               |
|---------------|----------|----------|-------------------------------------------|
| `id`          | `guid`   | No       | ID of the deleted category.               |
| `message`     | `string` | No       | Confirmation message.                     |

**Response `400 Bad Request` — category not found or already deleted:**

```json
{
  "isSuccess": false,
  "isFailure": true,
  "message": "Category not found.",
  "error": {
    "code": "NullValue",
    "message": "Value cannot be null.",
    "httpStatusCode": 400
  },
  "value": null
}
```

**Response `500 Internal Server Error`:**
Transaction is rolled back; common failure `Result` with `Error.ServerError`.

---

**Handler Logic Summary:**

1. Constructs `DeleteCategoryCommand(id)` (a `record` type with `Id` property).
2. Handler fetches the category by `Id` via `categoryRepository.GetByIdAsync`.
3. If entity is `null` or `IsDeleted == true`, returns `Result.Failure` with `Error.NullValue`.
4. Calls `category.SoftDelete(currentUserId)` — marks the entity as deleted (sets `IsDeleted = true`, sets `UpdatedBy`).
5. Begins a database transaction, marks the entity as updated (not removed), saves changes, commits.
6. Maps to `DeleteCategoryResponse` (includes `Id` and confirmation `Message`) and returns `Result.Success`.
7. On exception, rolls back the transaction and returns `Result.Failure` with `Error.ServerError`.
