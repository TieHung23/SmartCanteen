# SmartCanteen Category API

Base URL: `/api/categories`  
Auth: `[Authorize]` (any authenticated user)  
API Version: `1.0`

---

## `GET /api/categories`
**Query:** `?name=string&pageNumber=1&pageSize=10`

Paginated items:
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "imgUrl": "string | null"
}
```

## `GET /api/categories/{id}`
**200 Response:**
```json
{
  "value": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "imgUrl": "string | null",
    "createdAtUtc": "...",
    "updatedAtUtc": "... | null",
    "createdBy": "guid",
    "updatedBy": "guid"
  },
  "isSuccess": true
}
```
`404` if not found.

## `POST /api/categories`
**Content-Type:** `multipart/form-data`

**Fields:**
- `name` (string)
- `description` (string)
- `image` (file, optional) — uploaded to Cloudinary

**201 Response:** Returns `Location` header to `GET /api/categories/{id}`.
```json
{
  "value": { "id": "guid", "name": "string", "description": "string", "imgUrl": "string | null" },
  "isSuccess": true
}
```

## `PUT /api/categories/{id}`
Same `multipart/form-data` fields as create. Omitting `image` preserves existing `imgUrl`.

## `DELETE /api/categories/{id}`
Soft-delete. `404` if not found.
