# SmartCanteen Dish API

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

## Dishes

### `GET /api/dishes`
**Auth:** Authorize  
**Query:** `?name=string&categoryId=guid&isActive=bool&pageNumber=1&pageSize=10`

**Paginated response items:**
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "price": 0.0,
  "isActive": true,
  "categoryId": "guid",
  "imgUrl": "string | null"
}
```

### `GET /api/dishes/{id}`
**Auth:** Authorize  

**Response:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "price": 0.0,
    "isActive": true,
    "categoryId": "guid",
    "imgUrl": "string | null"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/dishes`
**Auth:** Authorize  
**Content-Type:** `multipart/form-data`

**Fields:**
- `name` (string)
- `description` (string)
- `price` (decimal)
- `categoryId` (guid)
- `image` (file, optional) — uploaded to Cloudinary, URL saved as `imgUrl`

**201 Response:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "price": 0.0,
    "isActive": true,
    "categoryId": "guid",
    "imgUrl": "string | null"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `PUT /api/dishes/{id}`
**Auth:** Authorize  
**Content-Type:** `multipart/form-data`

**Fields:**
- `name` (string)
- `description` (string)
- `price` (decimal)
- `isActive` (bool)
- `categoryId` (guid)
- `image` (file, optional) — uploaded to Cloudinary, URL saved as `imgUrl`; if omitted, existing `imgUrl` is preserved

**Response:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "price": 0.0,
    "isActive": true,
    "categoryId": "guid",
    "imgUrl": "string | null"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `DELETE /api/dishes/{id}`
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

