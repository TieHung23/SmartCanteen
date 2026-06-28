# SmartCanteen Refund Policy API

Base URL: `/api/refund-policies` (user) / `/api/manager/refund-policies` (manager)  
API Version: `1.0`

---

## User Endpoints

### `GET /api/refund-policies`
Auth: `[Authorize]`

Returns active refund policies:
```json
{
  "value": [
    {
      "code": "string",
      "name": "string",
      "description": "string",
      "percent": 25.0,
      "requiresImage": true,
      "isActive": true
    }
  ],
  "isSuccess": true,
  "message": "Refund policies retrieved successfully."
}
```

---

## Manager Endpoints

Auth: `[Authorize(Roles = "Manager")]` for all.

### `GET /api/manager/refund-policies`
Same response shape as user endpoint.

### `POST /api/manager/refund-policies`
```json
{
  "code": "string",
  "name": "string",
  "description": "string",
  "percent": 25.0,
  "requiresImage": true,
  "isActive": true
}
```
**201 Created** (empty Location).

### `PUT /api/manager/refund-policies/{code}`
```json
{
  "name": "string",
  "description": "string",
  "percent": 25.0,
  "requiresImage": true,
  "isActive": true
}
```

### `DELETE /api/manager/refund-policies/{code}`
`404` if not found.
