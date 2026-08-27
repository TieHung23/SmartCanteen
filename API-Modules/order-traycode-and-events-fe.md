# FE guide — Order TrayCode + Notification orderId + Order Events

> Cho các yêu cầu của **Trúc Ân** (staff xem TrayCode ở order detail; noti bấm vào mở order detail) và
> **filter log chi tiết theo đơn**. BE đã làm ở nhánh `feat/order-events-and-i18n` (PR → develop; kèm luôn Việt hoá message lỗi).
> ⚠️ **Chỉ dùng được SAU khi PR merge vào develop + deploy.** Trước đó server cũ chưa có.

Tất cả endpoint dưới đây yêu cầu role **Manager/Staff** (header `Authorization: Bearer <token>`).

---

## 1. Order detail giờ trả `trayCode`

`GET /api/manager/orders/{orderId}` — response **thêm field `trayCode`**:

```jsonc
{
  "value": {
    "id": "...",
    "status": 4,
    "totalPrice": 30000,
    "trayCode": "TRAY004",     // 👈 MỚI: mã khay đang gắn với đơn
    "items": [ ... ],
    "statusHistories": [ ... ]
  }
}
```

**Ý nghĩa `trayCode`:** mã khay **đang gắn với serving job MỚI NHẤT của đơn** — staff dựa vào để lấy đúng khay.

**⚠️ QUAN TRỌNG — khi nào `trayCode = null`:**
| Giai đoạn đơn | `trayCode` |
|---|---|
| Chưa vào phục vụ / chưa quét-bind khay | `null` |
| Robot đang ráp (đã bind khay) | ✅ có mã (vd `"TRAY004"`) |
| **Đã lên kệ (staff shelve xong)** | **`null`** — khay đã trả về pool để tái dùng |

→ Staff muốn thấy TrayCode thì xem **trong lúc đơn đang được phục vụ/chuẩn bị lên kệ**, KHÔNG phải sau khi đã lên kệ. FE nên **ẩn/hiện "N/A"** khi `trayCode == null`.

---

## 2. Notification bấm vào → mở order detail (KHÔNG cần đổi BE)

Notification **đã có sẵn** `referenceId` (= orderId) + `actionUrl`. Không cần BE thêm gì.

Response 1 notification (vd noti "Cánh tay đã gắp xong" `OrderAssembledStaff`):
```jsonc
{
  "id": "...",
  "type": "Order.AssembledStaff",
  "title": "Cánh tay đã gắp xong",
  "message": "Robot đã ráp xong đơn ... Quét khay để đưa lên kệ.",
  "referenceType": "Order",
  "referenceId": "36567f01-c7e4-4b92-8d8c-cf3b9b9f123a",  // 👈 = orderId
  "actionUrl": "/staff/orders/36567f01-c7e4-4b92-8d8c-cf3b9b9f123a", // 👈 sẵn link
  "isRead": false,
  "createdAtUtc": "..."
}
```

**FE làm:** khi bấm notification →
- Cách 1: điều hướng thẳng theo `actionUrl`.
- Cách 2: nếu `referenceType == "Order"` → route tới trang order detail bằng `referenceId`.

---

## 3. Log robot theo ĐƠN (thay vì theo job)

Trước đây chỉ có `GET /api/manager/serving-jobs/{jobId}/events` — cần **jobId**. Giờ có endpoint **theo orderId** (gom mọi job của đơn, kể cả các lượt requeue):

> 🔎 **Vì sao dialog "LỊCH SỬ SỰ KIỆN ROBOT (TIMELINE)" đang hiện "0 sự kiện"?**
> Vì dialog đang gọi endpoint **per-job** `GET /manager/serving-jobs/{jobId}/events` — endpoint đó **CHƯA có trên server** (nằm ở nhánh `feat/vietnamese-error-messages` chưa merge), nên FE gọi vào khoảng không → luôn rỗng.
> → **Đổi dialog sang gọi endpoint per-order bên dưới** (dialog đã có sẵn `orderId` — chính là "MÃ ĐƠN" hiển thị trên header). Per-order còn **đúng hơn** per-job: 1 đơn requeue sẽ đẻ **nhiều job**; job MỚI NHẤT có thể 0 event trong khi event thật nằm ở job cũ → per-job dễ hiện trống oan, per-order gom hết mọi job của đơn.

### `GET /api/manager/orders/{orderId}/events`
Query tùy chọn: `?type=<EventType>` — lọc theo loại sự kiện (bỏ trống = tất cả).

**Ví dụ:** `GET /api/manager/orders/36567f01-.../events`
```jsonc
{
  "value": {
    "orderId": "36567f01-...",
    "count": 5,
    "events": [
      {
        "servingJobId": "...",          // sự kiện thuộc job nào (phân biệt lượt requeue)
        "eventType": "PickStarted",
        "dishId": "2eb5d8c4-...",
        "dishName": "Cơm chay thập cẩm", // đã enrich sẵn, khỏi tra thêm
        "robotArmId": "...",
        "station": "S1",                 // mã tay robot
        "message": null,
        "occurredAt": "2026-08-27T11:37:37Z"
      },
      { "eventType": "PickCompleted", "dishName": "Cơm chay thập cẩm", "station": "S1", ... },
      { "eventType": "PlaceCompleted", "dishName": "Cơm chay thập cẩm", "station": "S1", ... },
      { "eventType": "Error", "message": "Robot: khay thiếu món ...", ... }
    ]
  }
}
```
- `events` đã **sắp theo thời gian** (`occurredAt` tăng dần).
- Đã **enrich `dishName` + `station`** → hiện thẳng "PlaceCompleted — Cơm chay thập cẩm — S1", khỏi tra thêm.

### Lọc theo loại — `?type=`
```
GET /api/manager/orders/{orderId}/events?type=Error          # chỉ lỗi
GET /api/manager/orders/{orderId}/events?type=PlaceCompleted # chỉ "đã đặt xong"
```
`type` không hợp lệ → bỏ lọc (trả tất cả).

**Các `eventType` có thể gặp:**
`JobReceived`, `PickStarted`, `PickCompleted`, `PlaceCompleted`, `Recovered`, `Error`, `Connected`, `Disconnected`, `EmergencyStop`.

---

## Tham chiếu — Order `status` (số)
| status | Ý nghĩa |
|---|---|
| 0 | Pending |
| 1 | ReadyForPickup |
| 2 | Completed |
| 3 | Cancelled |
| 4 | Preparing |
| 7 | Expired |

*(FE confirm lại với BE nếu cần — đây là giá trị enum hiện tại.)*

---

## Tóm cho FE
| Việc | Endpoint | Ghi chú |
|---|---|---|
| Xem TrayCode của đơn | `GET /manager/orders/{id}` → `trayCode` | null sau khi lên kệ — ẩn/N.A |
| Noti bấm → order detail | dùng `referenceId`/`actionUrl` sẵn có | KHÔNG cần BE đổi |
| Log robot theo đơn | `GET /manager/orders/{id}/events?type=` | gom mọi job, đã enrich món/tay |
