# Flow: Admin xem API Logs

> Hướng dẫn FE tích hợp tính năng Admin xem log API từ bảng `ApiLogs`.

---

## 1. Tổng quan

Mọi request HTTP vào hệ thống (ngoại trừ OPTIONS) đều được ghi vào bảng `ApiLogs` bởi `ApiLoggerMiddleware`. Admin có thể xem, lọc, và tìm kiếm các log này để debug hoặc giám sát.

**Lưu ý:** Middleware chỉ ghi log các request có path chứa `/api/` (bỏ qua Swagger, SignalR negotiate, v.v.).

---

## 2. Sequence Diagram

```
Admin (FE)                      Backend                         DB
    │                               │                            │
    │                               │  (Middleware ghi log tự động)
    │                               │  Mỗi request API           │
    │                               │───────────────────────────>│
    │                               │  INSERT INTO "ApiLogs"     │
    │                               │                            │
    │  1. Vào trang Logs            │                            │
    │──────────────────────────────>│                            │
    │                               │                            │
    │  2. Fetch danh sách logs      │                            │
    │  GET /api/admin/logs?page=1   │                            │
    │──────────────────────────────>│                            │
    │                               │  SELECT FROM "ApiLogs"     │
    │                               │───────────────────────────>│
    │                               │<───────────────────────────│
    │<──────────────────────────────│                            │
    │  Danh sách log + pagination   │                            │
    │                               │                            │
    │  3. Xem chi tiết log          │                            │
    │──────────────────────────────>│                            │
    │<──────────────────────────────│                            │
    │  Request body + response body │                            │
    │                               │                            │
    │  4. Lọc theo level/status     │                            │
    │──────────────────────────────>│                            │
    │<──────────────────────────────│                            │
    │  Kết quả lọc                  │                            │
```

---

## 3. API Endpoint (cần implement)

> Hiện tại backend chưa có endpoint public để query ApiLogs. Cần implement handler + controller.

### 3.1 List logs

```
GET /api/admin/logs
Auth: Admin
```

**Query params:**

| Param | Type | Mô tả |
|-------|------|-------|
| `pageNumber` | int | default 1 |
| `pageSize` | int | default 20, max 100 |
| `logLevel` | string | Lọc theo level: `INFO`, `ERROR`, `DEBUG` |
| `method` | string | Lọc theo HTTP method: `GET`, `POST`, `PUT`, `DELETE` |
| `url` | string | Tìm kiếm URL (contains) |
| `statusCodeMin` | int | Lọc response status >= (VD: 400 để xem lỗi) |
| `fromDate` | DateTimeOffset | Lọc từ ngày |
| `toDate` | DateTimeOffset | Lọc đến ngày |

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

## 4. UI/UX gợi ý

### Màn hình danh sách logs

```
┌─── 📋 API Logs ─────────────────────────────────────────────────────────┐
│                                                                         │
│  🔍 [______________]  Level: [All ▼]  Method: [All ▼]  Status: [≥__]  │
│  Từ: [__/__/____]  Đến: [__/__/____]  [Lọc] [Xóa lọc]                  │
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

**Màu sắc gợi ý theo level:**
| Level | Color | Ý nghĩa |
|-------|-------|---------|
| ERROR | 🔴 Đỏ | Lỗi server (5xx) hoặc exception |
| INFO | 🟢 Xanh | Thành công (2xx, 4xx) |
| DEBUG | 🟡 Vàng | Thông tin debug |

**Màu status code:**
| Status | Color |
|--------|-------|
| 2xx | 🟢 Xanh |
| 4xx | 🟡 Vàng |
| 5xx | 🔴 Đỏ |

### Màn hình chi tiết log

Khi click vào một log, mở drawer hoặc page:

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

| Filter | Behavior | Gợi ý UI |
|--------|----------|----------|
| `logLevel` | Multi-select dropdown | Checkbox: ERROR, INFO, DEBUG |
| `method` | Multi-select dropdown | Checkbox: GET, POST, PUT, DELETE |
| `url` | Text input, tìm kiếm contains | Search input với debounce 300ms |
| `statusCode` | Range input (min - max) | 2 input số hoặc select nhanh: 2xx, 3xx, 4xx, 5xx |
| `fromDate/toDate` | Date range picker | Date picker + time picker |
| `requestId` | Text input chính xác | Search input riêng |

---

## 6. Auto-refresh (real-time)

Nếu đang ở màn hình logs, có thể thêm auto-refresh:

```javascript
// Gợi ý: Polling 30s hoặc SignalR event khi có log mới
useEffect(() => {
  const interval = setInterval(() => {
    if (isAutoRefresh) fetchLogs();
  }, 30000);
  return () => clearInterval(interval);
}, [isAutoRefresh]);
```

---

## 7. Export (nâng cao)

Có thể thêm nút export để tải logs về CSV:

```
GET /api/admin/logs/export?fromDate=...&toDate=...&logLevel=ERROR
```

Trả về file CSV với headers:
```
Timestamp,Level,Method,URL,StatusCode,Duration(ms),IP,RequestId
```

---

## 8. Lưu ý cho FE

| Yếu tố | Ghi chú |
|--------|---------|
| `apiBody` / `apiResponse` | Có thể rất dài (>10KB) — chỉ fetch khi xem detail |
| `errorTrace` | Có thể null nếu request thành công |
| `createdDate` / `endDate` | Format ISO 8601, FE cần format theo timezone local |
| `requestId` | Dùng để trace request từ FE → BE → DB |
| Pagination | Mặc định 20 items/trang, không cho pageSize > 100 |
