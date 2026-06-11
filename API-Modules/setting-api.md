# SmartCanteen Setting API

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

## Settings

### `GET /api/settings`
**Auth:** Authorize  
**Query:** `?code=string&name=string&group=string&type=string&pageNumber=1&pageSize=10`

**Paginated response items:**
```json
{
  "id": "guid",
  "code": "string",
  "name": "string",
  "description": "string",
  "group": "string",
  "value": "string",
  "type": "string"
}
```

### `GET /api/settings/{id}`
**Auth:** Authorize  

**Response:**
```json
{
  "value": {
    "id": "guid",
    "code": "string",
    "name": "string",
    "description": "string",
    "group": "string",
    "value": "string",
    "type": "string"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `POST /api/settings`
**Auth:** Authorize  

**Request body:**
```json
{
  "code": "string",
  "name": "string",
  "description": "string | null",
  "group": "string",
  "value": "string",
  "type": "string"
}
```

**201 Response:**
```json
{
  "value": {
    "id": "guid",
    "code": "string",
    "name": "string",
    "description": "string",
    "group": "string",
    "value": "string",
    "type": "string"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `PUT /api/settings/{id}`
**Auth:** Authorize  

**Request body:**
```json
{
  "name": "string",
  "description": "string | null",
  "group": "string",
  "value": "string",
  "type": "string"
}
```

**Response:**
```json
{
  "value": {
    "id": "guid",
    "code": "string",
    "name": "string",
    "description": "string",
    "group": "string",
    "value": "string",
    "type": "string"
  },
  "isSuccess": true,
  "message": "string"
}
```

### `DELETE /api/settings/{id}`
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

