# SmartCanteen Dish API

Base URL: `/api/dishes`  
Auth: `[Authorize]` (any authenticated user)  
API Version: `1.0`  
Currency: **Point**

---

## `GET /api/dishes`
**Query:** `?name=string&categoryId=guid&isActive=bool&pageNumber=1&pageSize=10`

Paginated items:
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

## `GET /api/dishes/{id}`
**200 Response:**
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
  "isSuccess": true
}
```
`404` if not found.

## `POST /api/dishes`
**Content-Type:** `multipart/form-data`

**Fields:**
- `name` (string)
- `description` (string)
- `price` (decimal)
- `categoryId` (guid)
- `image` (file, optional) — uploaded to Cloudinary

**201 Response:** Returns `Location` header to `GET /api/dishes/{id}`.
```json
{
  "value": { "id": "guid", "name": "string", "description": "string", "price": 0.0, "isActive": true, "categoryId": "guid", "imgUrl": "string | null" },
  "isSuccess": true
}
```

## `PUT /api/dishes/{id}`
Same `multipart/form-data` fields plus `isActive` (bool). Omitting `image` preserves existing `imgUrl`.

## `DELETE /api/dishes/{id}`
`404` if not found.
