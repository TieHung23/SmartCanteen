# Dish Module API Documentation

Base URL: `/api/Dishes`
Global auth: All endpoints require a valid JWT bearer token (`[Authorize]` attribute on controller).

Every response is wrapped in a `Result<TValue>` envelope:

**Success envelope (HTTP 200/201):**
```json
{
  "isSuccess": true,
  "isFailure": false,
  "message": "Dish ... successfully.",
  "value": { /* TValue (see per-endpoint) */ },
  "error": null
}
```

**Failure envelope (HTTP 400/401/403/404/409/500):**
```json
{
  "isSuccess": false,
  "isFailure": true,
  "message": "Error description.",
  "value": null,
  "error": {
    "code": "ErrorCode",
    "message": "Error description.",
    "httpStatusCode": 400
  }
}
```

> The `error` object is serialized with `[JsonIgnore]` on `Code`, `Message`, and `HttpStatusCode` in the class definition. The actual wire format depends on the serializer configuration; the table below documents the *logical* fields.

---

## `GET /api/Dishes`
**Auth:** Required (JWT)

**Request — Query Parameters:**

| Param      | Type    | Required | Default | Description |
|------------|---------|----------|---------|-------------|
| PageNumber | int     | No       | 1       | Must be >= 1 (clamped) |
| PageSize   | int     | No       | 10      | Clamped to [1, 100] |
| Name       | string  | No       | null    | Case-insensitive substring filter on dish name |
| CategoryId | guid    | No       | null    | Exact-match category filter |
| IsActive   | bool    | No       | null    | Filter by active/inactive status |

```json
// Example query string
// GET /api/Dishes?PageNumber=1&PageSize=10&Name=pho&CategoryId=3fa85f64-5717-4562-b3fc-2c963f66afa6&IsActive=true
```

**Response 200:**
```json
{
  "isSuccess": true,
  "isFailure": false,
  "message": "Dishes retrieved successfully.",
  "value": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "Phở bò",
        "description": "Traditional Vietnamese beef noodle soup",
        "price": 55000,
        "isActive": true,
        "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "imgUrl": "https://res.cloudinary.com/.../image/upload/v1/dish/pho-bo.jpg"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  },
  "error": null
}
```

**Handler Logic:**
1. Build queryable from dish repository, filtering out soft-deleted dishes (`!x.IsDeleted`).
2. Apply optional filters: `Name` (case-insensitive `Contains`), `CategoryId` (exact), `IsActive` (exact).
3. Count total matching records.
4. Paginate: skip `(PageNumber - 1) * PageSize`, take `PageSize`, order by `Name` ascending.
5. Map to `GetAllDishesResponse` list and wrap in `PaginatedList<T>`.

**Status codes:**
- `200` — Success
- `400` — Validation failure (bad params)

---

## `GET /api/Dishes/{id:guid}`
**Auth:** Required (JWT)

**Request — Route Parameters:**

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| id    | guid | Yes      | Dish identifier |

**Response 200:**
```json
{
  "isSuccess": true,
  "isFailure": false,
  "message": "Dish retrieved successfully.",
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Phở bò",
    "description": "Traditional Vietnamese beef noodle soup",
    "price": 55000,
    "isActive": true,
    "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "imgUrl": "https://res.cloudinary.com/.../image/upload/v1/dish/pho-bo.jpg"
  },
  "error": null
}
```

**Response 404:**
```json
{
  "isSuccess": false,
  "isFailure": true,
  "message": "Dish not found.",
  "value": null,
  "error": {
    "code": "NullValue",
    "message": "Dish not found.",
    "httpStatusCode": 404
  }
}
```

**Handler Logic:**
1. Query dish by `Id` where `!x.IsDeleted`. Use `FirstOrDefaultAsync`.
2. If `null`, return `Result.Failure<GetDishByIdResponse>(Error.NullValue, "Dish not found.")` — controller maps to `NotFound()`.
3. Map to `GetDishByIdResponse` and return success.

**Status codes:**
- `200` — Found
- `404` — Dish not found or soft-deleted
- `500` — Server error

---

## `POST /api/Dishes`
**Auth:** Required (JWT)

**Content-Type:** `multipart/form-data`

**Request — Form Fields:**

| Field      | Type      | Required | Description |
|------------|-----------|----------|-------------|
| name       | string    | Yes      | Dish name |
| description| string    | Yes      | Dish description |
| price      | decimal   | Yes      | Price (>= 0) |
| categoryId | guid      | Yes      | Category identifier |
| image      | IFormFile | No       | Image file (uploaded to Cloudinary) |

```http
POST /api/Dishes
Content-Type: multipart/form-data; boundary=----boundary

------boundary
Content-Disposition: form-data; name="name"

Phở bò
------boundary
Content-Disposition: form-data; name="description"

Traditional Vietnamese beef noodle soup
------boundary
Content-Disposition: form-data; name="price"

55000
------boundary
Content-Disposition: form-data; name="categoryId"

3fa85f64-5717-4562-b3fc-2c963f66afa6
------boundary
Content-Disposition: form-data; name="image"; filename="pho-bo.jpg"
Content-Type: image/jpeg

[binary data]
------boundary--
```

**Validation Rules** (from `CreateDishCommandValidator`):

| Field       | Rule                        |
|-------------|-----------------------------|
| Name        | `NotEmpty`, `MaximumLength(200)` |
| Description | `NotEmpty`, `MaximumLength(500)` |
| Price       | `GreaterThanOrEqualTo(0)`   |
| CategoryId  | `NotEmpty`                  |

**Response 201:**
```json
{
  "isSuccess": true,
  "isFailure": false,
  "message": "Dish created successfully.",
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Phở bò",
    "description": "Traditional Vietnamese beef noodle soup",
    "price": 55000,
    "isActive": true,
    "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "imgUrl": "https://res.cloudinary.com/.../image/upload/v1/dish/pho-bo.jpg"
  },
  "error": null
}
```

**Handler Logic:**
1. If `image` is provided, upload to Cloudinary via `ICloundinaryUpload.UploadFileAsync(stream, fileName)` → obtains `imgUrl = uploadResult.ViewUrl` (secure URL).
2. Look up category by `CategoryId`. If category is `null` or `IsDeleted`, return failure `"Category not found."`.
3. Create a new `Dish` aggregate via `DishAggregateRoot.Create(name, description, Money.Create(price), categoryId, currentUserService.UserId, imgUrl)`.
4. Begin transaction → add dish → save → commit.
5. Map to `CreateDishResponse`, return `Result.Success` with 201 `CreatedAtAction` (pointing to `GetDishById`).

**Status codes:**
- `201` — Created (with `Location` header pointing to `GET /api/Dishes/{id}`)
- `400` — Validation failure or category not found
- `500` — Server error (transaction rolled back)

---

## `PUT /api/Dishes/{id:guid}`
**Auth:** Required (JWT)

**Content-Type:** `multipart/form-data`

**Request — Route Parameters:**

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| id    | guid | Yes      | Dish identifier to update |

**Request — Form Fields:**

| Field      | Type      | Required | Description |
|------------|-----------|----------|-------------|
| name       | string    | Yes      | Dish name |
| description| string    | Yes      | Dish description |
| price      | decimal   | Yes      | Price (>= 0) |
| isActive   | bool      | Yes      | Active status |
| categoryId | guid      | Yes      | Category identifier |
| image      | IFormFile | No       | New image file (replaces existing if uploaded) |

```http
PUT /api/Dishes/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: multipart/form-data; boundary=----boundary

------boundary
Content-Disposition: form-data; name="name"

Phở bò đặc biệt
------boundary
Content-Disposition: form-data; name="description"

Special beef noodle soup with extra toppings
------boundary
Content-Disposition: form-data; name="price"

65000
------boundary
Content-Disposition: form-data; name="isActive"

true
------boundary
Content-Disposition: form-data; name="categoryId"

3fa85f64-5717-4562-b3fc-2c963f66afa6
------boundary
Content-Disposition: form-data; name="image"; filename="pho-bo-special.jpg"
Content-Type: image/jpeg

[binary data]
------boundary--
```

**Validation Rules** (from `UpdateDishCommandValidator`):

| Field       | Rule                        |
|-------------|-----------------------------|
| Id          | `NotEmpty`                  |
| Name        | `NotEmpty`, `MaximumLength(200)` |
| Description | `NotEmpty`, `MaximumLength(500)` |
| Price       | `GreaterThanOrEqualTo(0)`   |
| CategoryId  | `NotEmpty`                  |

> Note: `IsActive` has no FluentValidation rule but is required in the form (defaults to `true` in the command class).

**Response 200:**
```json
{
  "isSuccess": true,
  "isFailure": false,
  "message": "Dish updated successfully.",
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Phở bò đặc biệt",
    "description": "Special beef noodle soup with extra toppings",
    "price": 65000,
    "isActive": true,
    "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "imgUrl": "https://res.cloudinary.com/.../image/upload/v1/dish/pho-bo-special.jpg"
  },
  "error": null
}
```

**Handler Logic:**
1. If `image` is provided, upload to Cloudinary → `imgUrl`.
2. Look up existing dish by `Id`. If `null` or `IsDeleted`, return failure `"Dish not found."`.
3. Look up category by `CategoryId`. If `null` or `IsDeleted`, return failure `"Category not found."`.
4. Call `dish.Update(name, description, Money.Create(price), categoryId, isActive, currentUserService.UserId, imgUrl ?? dish.ImgUrl)`. Falls back to existing `ImgUrl` if no new image provided.
5. Begin transaction → mark dish as updated → save → commit.
6. Map to `UpdateDishResponse`, return success.

**Status codes:**
- `200` — Updated
- `400` — Validation failure or category not found
- `404` — Dish not found / soft-deleted
- `500` — Server error (transaction rolled back)

---

## `DELETE /api/Dishes/{id:guid}`
**Auth:** Required (JWT)

**Request — Route Parameters:**

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| id    | guid | Yes      | Dish identifier to delete |

**Response 200:**
```json
{
  "isSuccess": true,
  "isFailure": false,
  "message": "Dish deleted successfully.",
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "message": "Dish deleted successfully."
  },
  "error": null
}
```

**Response 404:**
```json
{
  "isSuccess": false,
  "isFailure": true,
  "message": "Dish not found.",
  "value": null,
  "error": {
    "code": "NullValue",
    "message": "Dish not found.",
    "httpStatusCode": 404
  }
}
```

**Handler Logic:**
1. Look up dish by `Id`. If `null` or `IsDeleted`, return failure `"Dish not found."`.
2. Call `dish.SoftDelete(currentUserService.UserId)` — sets `IsDeleted = true`, `IsActive = false`, `UpdatedAtUtc`, `UpdatedBy`.
3. Begin transaction → update dish → save → commit.
4. Return `DeleteDishResponse` (just `Id` and success message).

**Status codes:**
- `200` — Soft-deleted
- `404` — Dish not found / already deleted
- `500` — Server error (transaction rolled back)

---

## Shared Schemas

### Dish Response Object (used in Create/Update/GetById/GetAll)
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "price": "decimal",
  "isActive": "boolean",
  "categoryId": "guid",
  "imgUrl": "string | null"
}
```

### PaginatedList Envelope (used in GetAll)
```json
{
  "items": [ "DishResponseObject" ],
  "pageNumber": "int",
  "pageSize": "int",
  "totalCount": "int",
  "totalPages": "int",
  "hasPreviousPage": "boolean",
  "hasNextPage": "boolean"
}
```

### DeleteDishResponse
```json
{
  "id": "guid",
  "message": "string"
}
```

### Error Object
```json
{
  "code": "string",
  "message": "string",
  "httpStatusCode": "int"
}
```

---

## Error Codes Reference (dish-relevant)

| Code          | HTTP Status | Meaning                         |
|---------------|-------------|----------------------------------|
| NullValue     | 404/400     | Dish or Category not found       |
| ServerError   | 500         | Unexpected server error          |
| (Validation)  | 400         | FluentValidation failures        |

---

## Quick Reference Table

| Method | Route                  | Auth | Content-Type       | Success | Failure |
|--------|------------------------|------|--------------------|---------|---------|
| GET    | /api/Dishes            | JWT  | —                  | 200     | 400     |
| GET    | /api/Dishes/{id}       | JWT  | —                  | 200     | 404     |
| POST   | /api/Dishes            | JWT  | multipart/form-data | 201     | 400     |
| PUT    | /api/Dishes/{id}       | JWT  | multipart/form-data | 200     | 400/404 |
| DELETE | /api/Dishes/{id}       | JWT  | —                  | 200     | 404     |
