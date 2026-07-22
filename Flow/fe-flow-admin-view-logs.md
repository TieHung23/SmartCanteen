# Flow: Admin Views API Logs

> FE integration guide for the Admin feature to view API logs from the `ApiLogs` table.

---

## 1. Overview

Every HTTP request to the system (except OPTIONS) is written to the `ApiLogs` table by `ApiLoggerMiddleware`. Admins can view, filter, and search these logs for debugging or monitoring.

**Note:** The middleware only logs requests whose path contains `/api/` (Swagger, SignalR negotiate, etc. are skipped).

---

## 2. Sequence Diagram

```
Admin (FE)                      Backend                         DB
    │                               │                            │
    │                               │  (Middleware logs automatically)
    │                               │  Every API request         │
    │                               │───────────────────────────>│
    │                               │  INSERT INTO "ApiLogs"     │
    │                               │                            │
    │  1. Open the Logs page        │                            │
    │──────────────────────────────>│                            │
    │                               │                            │
    │  2. Fetch the log list        │                            │
    │  GET /api/admin/logs?page=1   │                            │
    │──────────────────────────────>│                            │
    │                               │  SELECT FROM "ApiLogs"     │
    │                               │───────────────────────────>│
    │                               │<───────────────────────────│
    │<──────────────────────────────│                            │
    │  Log list + pagination        │                            │
    │                               │                            │
    │  3. View log detail           │                            │
    │──────────────────────────────>│                            │
    │<──────────────────────────────│                            │
    │  Request body + response body │                            │
    │                               │                            │
    │  4. Filter by level/status    │                            │
    │──────────────────────────────>│                            │
    │<──────────────────────────────│                            │
    │  Filtered results             │                            │
```

---

## 3. API Endpoint (to be implemented)

> The backend does not yet expose a public endpoint to query ApiLogs. A handler + controller need to be implemented.

### 3.1 List logs

```
GET /api/admin/logs
Auth: Admin
```

**Query params:**

| Param | Type | Description |
|-------|------|-------------|
| `pageNumber` | int | default 1 |
| `pageSize` | int | default 20, max 100 |
| `logLevel` | string | Filter by level: `INFO`, `ERROR`, `DEBUG` |
| `method` | string | Filter by HTTP method: `GET`, `POST`, `PUT`, `DELETE` |
| `url` | string | URL search (contains) |
| `statusCodeMin` | int | Filter response status >= (e.g. 400 to see errors) |
| `fromDate` | DateTimeOffset | Filter from date |
| `toDate` | DateTimeOffset | Filter to date |

**Paginated response items:**
```json
{
  "items": [
    {
      "id": "guid",
      "logLevel": "INFO",
      "apiUrl": "/api/orders",
      "apiMethod": "POST",
      "message": "Responded 200 in 45ms",
      "statusCode": 200,
      "localIpAddress": "192.168.1.100",
      "requestId": "abc-def",
      "createdDate": "2026-06-20T10:30:00Z",
      "endDate": "2026-06-20T10:30:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1523,
  "totalPages": 77,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

### 3.2 Get log detail

```
GET /api/admin/logs/{id}
Auth: Admin
```

**Response:**
```json
{
  "value": {
    "id": "guid",
    "loginId": "user-email@example.com | null",
    "logLevel": "INFO",
    "apiUrl": "/api/orders",
    "apiMethod": "POST",
    "apiBody": "{\"sessionId\":\"guid\",\"cartVersion\":1}",
    "apiResponse": "{\"value\":{\"id\":\"guid\",...}}",
    "message": "Responded 200 in 45ms",
    "errorTrace": null,
    "localIpAddress": "192.168.1.100",
    "requestId": "abc-def",
    "createdDate": "2026-06-20T10:30:00Z",
    "endDate": "2026-06-20T10:30:00Z"
  },
  "isSuccess": true
}
```

---

## 4. Suggested UI/UX

### Log list screen

```
┌─── 📋 API Logs ─────────────────────────────────────────────────────────┐
│                                                                         │
│  🔍 [______________]  Level: [All ▼]  Method: [All ▼]  Status: [≥__]  │
│  From: [__/__/____]  To: [__/__/____]  [Filter] [Clear]                │
│                                                                         │
│  ┌──────┬──────────┬────────────────────────────────┬───────┬──────────┐
│  │ Time │ Level    │ URL                            │Method│ Status   │
│  ├──────┼──────────┼────────────────────────────────┼───────┼──────────┤
│  │10:30 │ 🟢 INFO  │ /api/orders                    │ POST │ 200      │
│  │10:29 │ 🟢 INFO  │ /api/dishes                    │ GET  │ 200      │
│  │10:28 │ 🔴 ERROR │ /api/payments/webhook/sepay    │ POST │ 502      │
│  │10:27 │ 🟡 DEBUG │ /api/orders/xxx                │ GET  │ 200      │
│  └──────┴──────────┴────────────────────────────────┴───────┴──────────┘
│                                                  << < 1 2 3 ... 77 > >>│
└─────────────────────────────────────────────────────────────────────────┘
```

**Suggested colors by level:**
| Level | Color | Meaning |
|-------|-------|---------|
| ERROR | 🔴 Red | Server error (5xx) or exception |
| INFO | 🟢 Green | Success (2xx, 4xx) |
| DEBUG | 🟡 Yellow | Debug information |

**Status code colors:**
| Status | Color |
|--------|-------|
| 2xx | 🟢 Green |
| 4xx | 🟡 Yellow |
| 5xx | 🔴 Red |

### Log detail screen

When clicking a log, open a drawer or page:

```
┌─── 📄 Log Detail ─────────────────────────────────────────────────────┐
│                                                                       │
│  🔴 ERROR                                   2026-06-20 10:28:15      │
│  ──────────────────────────────────────────────────────────────────   │
│                                                                       │
│  Request                                                              │
│  ───────                                                              │
│  POST /api/payments/webhook/sepay                                     │
│  IP: 103.1.2.3                                                        │
│  Body:                                                                │
│  ┌────────────────────────────────────────────────────────────────┐   │
│  │ {"gatewayOrderId":"SEPAY123","amount":50000}                   │   │
│  └────────────────────────────────────────────────────────────────┘   │
│                                                                       │
│  Response                                                             │
│  ────────                                                             │
│  Status: 502                                                          │
│  Body:                                                                │
│  ┌────────────────────────────────────────────────────────────────┐   │
│  │ {"isSuccess":false,"error":"BadGateway", ...}                  │   │
│  └────────────────────────────────────────────────────────────────┘   │
│                                                                       │
│  Error Trace                                                          │
│  ┌────────────────────────────────────────────────────────────────┐   │
│  │ System.Net.Http.HttpRequestException: ...                      │   │
│  │    at SC.Infrastructure.Services.Payment.SePayWebhookVerifier  │   │
│  └────────────────────────────────────────────────────────────────┘   │
│                                                                       │
│  Metadata                                                             │
│  ────────                                                             │
│  RequestId: abc-def-123                                               │
│  Duration: 25023ms                                                    │
│  LoginId: admin@example.com                                           │
│                                                                       │
│  [Close]                                                              │
└───────────────────────────────────────────────────────────────────────┘
```

---

## 5. Filter/Search behavior

| Filter | Behavior | UI suggestion |
|--------|----------|---------------|
| `logLevel` | Multi-select dropdown | Checkboxes: ERROR, INFO, DEBUG |
| `method` | Multi-select dropdown | Checkboxes: GET, POST, PUT, DELETE |
| `url` | Text input, contains search | Search input with 300ms debounce |
| `statusCode` | Range input (min - max) | 2 number inputs or quick select: 2xx, 3xx, 4xx, 5xx |
| `fromDate/toDate` | Date range picker | Date picker + time picker |
| `requestId` | Exact-match text input | Separate search input |

---

## 6. Auto-refresh (real-time)

While on the logs screen, auto-refresh can be added:

```javascript
// Suggestion: 30s polling, or a SignalR event when a new log arrives
useEffect(() => {
  const interval = setInterval(() => {
    if (isAutoRefresh) fetchLogs();
  }, 30000);
  return () => clearInterval(interval);
}, [isAutoRefresh]);
```

---

## 7. Export (advanced)

An export button can be added to download logs as CSV:

```
GET /api/admin/logs/export?fromDate=...&toDate=...&logLevel=ERROR
```

Returns a CSV file with headers:
```
Timestamp,Level,Method,URL,StatusCode,Duration(ms),IP,RequestId
```

---

## 8. Notes for FE

| Item | Note |
|------|------|
| `apiBody` / `apiResponse` | Can be very large (>10KB) — only fetch when viewing detail |
| `errorTrace` | May be null when the request succeeded |
| `createdDate` / `endDate` | ISO 8601 format; FE must format to the local timezone |
| `requestId` | Used to trace a request from FE → BE → DB |
| Pagination | Default 20 items/page; pageSize must not exceed 100 |
