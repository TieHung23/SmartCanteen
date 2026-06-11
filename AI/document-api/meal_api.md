# Meals API Documentation

Base URL: `/api/Meals`  
API Version: `1.0`  
Content-Type: `application/json`

All endpoints (except `GET *`) require a valid JWT bearer token via the `Authorization` header.

---

## `GET /api/Meals`
**Auth:** `[AllowAnonymous]` — no token required.

**Description:** Returns a paginated list of meals, with optional name and active status filtering.

### Request — Query Parameters

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `PageNumber` | int | `1` | Page index (clamped to >= 1) |
| `PageSize` | int | `10` | Items per page (clamped 1–100) |
| `Name` | string | `null` | Partial name search (case-insensitive `Contains`) |
| `IsActive` | bool | `null` | Filter by active status |

### Validation / Clamping (in `PaginationParams`)

- `PageNumber`: clamped to minimum `1`
- `PageSize`: clamped to `1`–`100`; default `10`

### Response 200

```json
{
  "message": "Meals retrieved successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "string",
        "description": "string",
        "isActive": true,
        "availableFrom": "2025-06-10T00:00:00+00:00",
        "availableTo": "2025-06-10T00:00:00+00:00",
        "availableForOrder": "2025-06-10T00:00:00+00:00",
        "dishes": [
          {
            "dishId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "quantity": 1
          }
        ]
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 0,
    "totalPages": 0,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

### Response 400

```json
{
  "message": "An error occurred while retrieving meals.",
  "isSuccess": false,
  "isFailure": true,
  "error": { "code": "ServerError", "message": "An unexpected server error occurred.", "httpStatusCode": 500 },
  "value": null
}
```

### Handler Logic (`GetAllMealsQueryHandler`)

1. Starts with a queryable from `mealRepository.GetQueryable()`.
2. If `Name` is provided, filters with `x.Name.Contains(request.Name, OrdinalIgnoreCase)`.
3. If `IsActive` has a value, filters `x.IsActive == request.IsActive`.
4. Counts total matching records.
5. Computes skip = `(PageNumber - 1) * PageSize`.
6. Includes `DishMeals`, orders by `Name`, skips, takes, and materialises.
7. Maps each meal entity to `GetAllMealsResponse` containing the flat meal fields and a `DishMealDto` list.
8. Wraps everything in `PaginatedList<GetAllMealsResponse>` and returns `Result.Success`.

---

## `GET /api/Meals/{id:guid}`
**Auth:** `[AllowAnonymous]` — no token required.

**Description:** Returns a single meal by its GUID, including meal templates, settings, and dishes.

### Request — Route Parameters

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid (route) | The meal's unique identifier |

### Response 200

```json
{
  "message": "Meal retrieved successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "string",
    "description": "string",
    "isActive": true,
    "availableFrom": "2025-06-10T00:00:00+00:00",
    "availableTo": "2025-06-10T00:00:00+00:00",
    "availableForOrder": "2025-06-10T00:00:00+00:00",
    "mealTemplates": [
      {
        "name": "string",
        "settings": [
          {
            "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "minQuantity": 0,
            "maxQuantity": 10,
            "isRequired": true
          }
        ]
      }
    ],
    "dishes": [
      {
        "dishId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "quantity": 1
      }
    ]
  }
}
```

### Response 404

```json
{
  "message": "Meal with id {id} not found.",
  "isSuccess": false,
  "isFailure": true,
  "error": { "code": "NullValue", "message": "Value cannot be null.", "httpStatusCode": 400 },
  "value": null
}
```

### Handler Logic (`GetMealByIdQueryHandler`)

1. Queries `mealRepository.GetQueryable(x => x.Id == request.Id)`.
2. Eagerly loads `MealTemplates`, then `MealTemplates.Settings`, and `DishMeals`.
3. If meal is `null`, returns `Result.Failure` with `Error.NullValue`.
4. Maps to `GetMealByIdResponse` including all nested `MealTemplateDto` → `MealSettingDto` and `DishMealDto` collections.

---

## `POST /api/Meals`
**Auth:** `[Authorize]` — JWT bearer required.

**Description:** Creates a new meal with associated meal templates (and their settings) and linked dishes.

### Request — JSON Body

```json
{
  "name": "string",
  "description": "string",
  "availableFrom": "2025-06-10T00:00:00Z",
  "availableTo": "2025-06-10T00:00:00Z",
  "availableForOrder": "2025-06-10T00:00:00Z",
  "mealTemplates": [
    {
      "name": "string",
      "settings": [
        {
          "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "minQuantity": 0,
          "maxQuantity": 10,
          "isRequired": true
        }
      ]
    }
  ],
  "dishes": [
    {
      "dishId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "quantity": 1
    }
  ]
}
```

### Validation Rules (inline in handler — no FluentValidation)

| Field | Rule |
|-------|------|
| `name` | Required; must not be null/whitespace |
| `mealTemplates[].name` | Required; must not be null/whitespace |
| `mealTemplates[].settings[].minQuantity` | Must be >= 0 |
| `mealTemplates[].settings[].maxQuantity` | Must be >= `minQuantity` |
| `dishes[].dishId` | Must reference an existing, non-deleted, active `Dish` |
| `dishes[].quantity` | If <= 0, defaults to `1` |

### Response 201

```json
{
  "message": "Meal created successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "string",
    "message": "Meal created successfully."
  }
}
```

The `Location` header points to `GET /api/Meals/{id}`.

### Response 400

```json
{
  "message": "Meal name is required.",
  "isSuccess": false,
  "isFailure": true,
  "error": { "code": "InvalidValue", "message": "Value is invalid.", "httpStatusCode": 400 },
  "value": null
}
```

Other failure messages: `"Template name is required."`, `"MinQuantity for category {id} cannot be negative."`, `"MaxQuantity for category {id} must be >= MinQuantity."`, `"Dish with id {id} not found or inactive."`, `"An error occurred while creating the meal."`.

### Handler Logic (`CreateMealCommandHandler`)

1. Validates `Name` is not empty.
2. Gets the current user ID from `ICurrentUserService`.
3. Creates the `Meal` aggregate root via `MealAggregateRoot.Create(...)`.
4. For each `MealTemplateInput`:
   - Validates template name.
   - Creates a `MealTemplate` entity.
   - For each `MealSettingInput`, validates min/max quantity and adds the setting.
   - Adds template to meal.
5. For each `DishMealInput`:
   - Fetches the dish from `dishRepository`.
   - Returns failure if dish is null, deleted, or inactive.
   - Creates `DishMeal` (with quantity defaulting to `1` if `<= 0`).
6. Wraps everything in a transaction: `BeginTransactionAsync` → `AddAsync` → `SaveChangesAsync` → `CommitAsync`.
7. On exception: rolls back, logs error, returns `ServerError`.

---

## `PUT /api/Meals/{id:guid}`
**Auth:** `[Authorize]` — JWT bearer required.

**Description:** Fully replaces an existing meal's data, including its templates, settings, and dish links.

### Request — Route Parameters

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid (route) | Must match `Id` in body, otherwise **400** is returned. |

### Request — JSON Body

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "string",
  "description": "string",
  "isActive": true,
  "availableFrom": "2025-06-10T00:00:00Z",
  "availableTo": "2025-06-10T00:00:00Z",
  "availableForOrder": "2025-06-10T00:00:00Z",
  "mealTemplates": [
    {
      "name": "string",
      "settings": [
        {
          "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "minQuantity": 0,
          "maxQuantity": 10,
          "isRequired": true
        }
      ]
    }
  ],
  "dishes": [
    {
      "dishId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "quantity": 1
    }
  ]
}
```

> **Note:** `PUT` includes an `isActive` field (unlike `POST`).

### Validation Rules (inline in handler + controller)

| Field | Rule |
|-------|------|
| Route `id` vs body `id` | **Controller-level:** must match, or returns `400` with `"ID mismatch between route and request body."` |
| `name` | Required; must not be null/whitespace |
| `mealTemplates[].name` | Required; must not be null/whitespace |
| `mealTemplates[].settings[].minQuantity` | Must be >= 0 |
| `mealTemplates[].settings[].maxQuantity` | Must be >= `minQuantity` |
| `dishes[].dishId` | Must reference an existing, non-deleted, active `Dish` |
| `dishes[].quantity` | If <= 0, defaults to `1` |

### Response 200

```json
{
  "message": "Meal updated successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "string",
    "message": "Meal updated successfully."
  }
}
```

### Response 400

Two shapes exist:

**ID mismatch (controller-level):**
```json
{
  "message": "ID mismatch between route and request body."
}
```

**Domain validation failure:**
```json
{
  "message": "Meal with id {id} not found.",
  "isSuccess": false,
  "isFailure": true,
  "error": { "code": "NullValue", "message": "Value cannot be null.", "httpStatusCode": 400 },
  "value": null
}
```

### Handler Logic (`UpdateMealCommandHandler`)

1. Fetches the existing meal by `request.Id`; returns `NullValue` failure if not found.
2. Validates `Name` is not empty.
3. Gets the current user ID.
4. Calls `meal.Update(...)` with all scalar fields, including `isActive`.
5. **Clears existing meal templates** (`meal.ClearMealTemplates()`) and rebuilds them from the request (same validation as create).
6. **Clears existing dish meals** (`meal.DishMeals.Clear()`) and rebuilds them from the request (same validation as create — dish existence + active check).
7. Wraps the update in a transaction: `BeginTransactionAsync` → `Update` → `SaveChangesAsync` → `CommitAsync`.
8. On exception: rolls back, logs error, returns `ServerError`.

---

## `DELETE /api/Meals/{id:guid}`
**Auth:** `[Authorize]` — JWT bearer required.

**Description:** Soft-deletes a meal by its ID.

### Request — Route Parameters

| Param | Type | Description |
|-------|------|-------------|
| `id` | Guid (route) | The meal's unique identifier |

### Response 200

```json
{
  "message": "Meal deleted successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": null,
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "message": "Meal deleted successfully (soft delete)."
  }
}
```

### Response 400/404

```json
{
  "message": "Meal with id {id} not found.",
  "isSuccess": false,
  "isFailure": true,
  "error": { "code": "NullValue", "message": "Value cannot be null.", "httpStatusCode": 400 },
  "value": null
}
```

### Handler Logic (`DeleteMealCommandHandler`)

1. Fetches the meal by `request.Id`; returns `NullValue` failure if not found (`404` via `NotFound()` in controller).
2. Calls `meal.SoftDelete()` (sets `IsDeleted = true` / `IsActive = false` on the entity).
3. Wraps the update in a transaction: `BeginTransactionAsync` → `Update` → `SaveChangesAsync` → `CommitAsync`.
4. Returns `DeleteMealResponse` with a confirmation message.

---

## Common Envelope

Every response is wrapped in a `Result<TValue>` envelope:

| Field | Type | Description |
|-------|------|-------------|
| `message` | string (nullable) | Human-readable result message |
| `isSuccess` | bool | `true` for 2xx responses |
| `isFailure` | bool | Inverse of `isSuccess` |
| `error` | object (nullable) | Null on success; on failure contains `code`, `message`, `httpStatusCode` |
| `value` | TValue (nullable) | The response payload; null on failure |

The controller maps the `Result` to HTTP status codes as follows:

- `Ok(result)` → **200** (success)
- `CreatedAtAction(..., result)` → **201** (success)
- `BadRequest(result)` → **400** (failure — validation, not found, etc.)
- `NotFound(result)` → **404** (failure — entity not found)

No content-type other than `application/json` is used.
