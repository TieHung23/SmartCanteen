# SmartCanteen Robot Serving API

Base URL: `/api/robot/serving-jobs`  
Auth: `[Authorize]`  
API Version: `1.0`

Creates a serving job for a paid order and pushes it via SignalR to the robot service. Idempotent — returns existing job if already active.

---

## `POST /api/robot/serving-jobs`

```json
{ "orderId": "guid", "trayId": "guid | null" }
```

```json
{
  "value": {
    "servingJobId": "guid",
    "orderId": "guid",
    "trayId": "guid | null",
    "status": "string"
  },
  "isSuccess": true
}
```
