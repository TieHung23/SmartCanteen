# SmartCanteen Setting API

Base URL: `/api/settings`  
Auth: `[Authorize(Roles = "Manager")]` (Manager only)  
API Version: `1.0`

---

## `GET /api/settings`
**Query:** `?code=string&name=string&group=string&type=string&pageNumber=1&pageSize=10`

Paginated items:
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

## `GET /api/settings/{id}`
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
  "isSuccess": true
}
```
`404` if not found.

## `POST /api/settings`
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
**201 Response:** Returns `Location` header.

## `PUT /api/settings/{id}`
`code` is not updatable (not in body).
```json
{
  "name": "string",
  "description": "string | null",
  "group": "string",
  "value": "string",
  "type": "string"
}
```

## `DELETE /api/settings/{id}`
`404` if not found.
