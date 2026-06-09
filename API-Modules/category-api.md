# SmartCanteen Category API

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

## Categories

### `GET /api/categories`
**Auth:** Authorize  
**Query:** `?name=string&pageNumber=1&pageSize=10`

**Paginated response items:**
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "imgUrl": "string | null"
}
```

### `GET /api/categories/{id}`
**Auth:** Authorize  

**Response:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "imgUrl": "string | null",
    "createdAtUtc": "2024-01-01T00:00:00Z",
    "updatedAtUtc": "2024-01-01T00:00:00Z | null",
    "createdBy": "guid",
    "updatedBy": "guid"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/categories`
**Auth:** Authorize  
**Content-Type:** `multipart/form-data`

**Fields:**
- `name` (string)
- `description` (string)
- `image` (file, optional) — uploaded to Cloudinary, URL saved as `imgUrl`

**201 Response:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "imgUrl": "string | null"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `PUT /api/categories/{id}`
**Auth:** Authorize  
**Content-Type:** `multipart/form-data`

**Fields:**
- `name` (string)
- `description` (string)
- `image` (file, optional) — uploaded to Cloudinary, URL saved as `imgUrl`; if omitted, existing `imgUrl` is preserved

**Response:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "imgUrl": "string | null"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `DELETE /api/categories/{id}` (soft delete)
**Auth:** Authorize  

**Response:**
```json
{
  "value": {
    "id": "guid",
    "message": "string"
  },
  "isSuccess": true,
  "message": "string"
}
```

---

