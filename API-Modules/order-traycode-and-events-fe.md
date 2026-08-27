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

### 🐞 Vì sao dialog "Chi tiết" ĐANG hiện JOB ID (mà list ngoài là mã đơn)?
Vì dialog cũ được viết cho endpoint **per-job** — nó phải **tự đi tìm 1 jobId** rồi hiện JOB ID lên header + gọi `serving-jobs/{jobId}/events`. Đó là lý do trải nghiệm lệch: **list ngoài theo ĐƠN, mở ra lại theo JOB**.
→ Với endpoint mới **bỏ hẳn jobId khỏi cách gọi**: dialog nhận thẳng **orderId** (chính là "MÃ ĐƠN HÀNG" của dòng vừa bấm), header hiện **MÃ ĐƠN**, còn *job* trở thành **nhóm con bên trong** (1 đơn requeue có nhiều lần chạy → nhiều job). Nhất quán từ ngoài vào trong.

### `GET /api/manager/orders/{orderId}/events`
- **Method:** GET · **Path param:** `orderId` (lấy từ dòng list, KHÔNG cần jobId nữa)
- **Query (optional):** `type=<EventType>` — lọc theo loại; bỏ trống = tất cả
- **Header:** `Authorization: Bearer <token>` (role Manager/Staff)

**Gọi mẫu (FE):**
```ts
async function getOrderEvents(orderId: string, type?: string) {
  const qs = type ? `?type=${type}` : "";
  const res = await api.get(`/api/manager/orders/${orderId}/events${qs}`);
  return res.data.value;        // { orderId, count, events: [...] }
}
// Trong dialog: onOpen(row) => getOrderEvents(row.orderId)   // dùng orderId, bỏ jobId
```

**Response — schema đầy đủ:**
```jsonc
{
  "value": {
    "orderId": "36567f01-...",   // = orderId truyền vào → HIỆN CÁI NÀY lên header dialog
    "count": 5,                  // tổng số event (đã tính cả filter nếu có)
    "events": [
      {
        "servingJobId": "05b47e63-...", // event thuộc LẦN CHẠY (job) nào → GOM NHÓM theo field này
        "eventType": "PickStarted",     // loại sự kiện (bảng bên dưới)
        "dishId": "2eb5d8c4-...",       // nullable — event mức job (JobReceived...) không có món
        "dishName": "Cơm chay thập cẩm",// nullable — ĐÃ enrich sẵn, hiện thẳng khỏi tra Dish API
        "robotArmId": "8f1c-...",       // nullable
        "station": "S1",                // nullable — mã tay robot (S1/S2/S3), enrich sẵn
        "message": null,                // nullable — thường chỉ Error mới có (lý do)
        "occurredAt": "2026-08-27T11:37:37Z" // ISO-8601, timeline sắp TĂNG DẦN theo cái này
      }
      // ... các event còn lại
    ]
  }
}
```

| Field | Kiểu | Null? | Dùng để |
|---|---|---|---|
| `orderId` | guid | không | Header dialog ("MÃ ĐƠN") |
| `count` | int | không | Badge "N sự kiện" |
| `events[].servingJobId` | guid | không | **Gom nhóm** theo lần chạy |
| `events[].eventType` | string | không | Icon + nhãn (bảng dưới) |
| `events[].dishId` / `dishName` | guid / string | **có** | Tên món; null = event mức job |
| `events[].robotArmId` / `station` | guid / string | **có** | Tay nào làm (S1/S2/S3) |
| `events[].message` | string | **có** | Lý do (nhất là Error) |
| `events[].occurredAt` | datetime | không | Thời gian + thứ tự |

### 🎨 Cách trình bày dialog (thay cho layout JOB ID cũ)
**Header:** hiện **MÃ ĐƠN** = `orderId` (+ badge `count` sự kiện). Bỏ ô "JOB ID".

**Thân — gom `events` theo `servingJobId`** (vì 1 đơn requeue có nhiều job). Mỗi nhóm = **"Lần chạy #k"** (k đánh theo thứ tự thời gian sớm→muộn của nhóm); trong nhóm sắp theo `occurredAt`:
```
MÃ ĐƠN 3b494808…                                   [5 sự kiện]
├─ Lần chạy 1  (job 05b47e63…)
│   ⚪ 11:37:37  Nhận việc
│   🔵 11:37:40  Bắt đầu gắp   · Cơm chay thập cẩm · S1
│   🟢 11:38:01  Đặt lên khay  · Cơm chay thập cẩm · S1
│   🔴 11:38:25  Lỗi           · "Robot: khay thiếu món…"   ← message hiện ở đây
└─ Lần chạy 2  (job a1b2c3…)
    🔵 11:40:10  Bắt đầu gắp   · Cơm chay thập cẩm · S1
    🟢 11:40:33  Đặt lên khay  · Cơm chay thập cẩm · S1
```
Mỗi dòng = `icon` + `occurredAt` + **nhãn eventType** + `dishName` (nếu có) + `station` (nếu có); riêng **Error** hiện thêm `message`.
Nếu đơn chỉ 1 job → chỉ 1 nhóm (khỏi cần chữ "Lần chạy", hiện phẳng cũng được).

**Map `eventType` → nhãn + màu gợi ý:**
| eventType | Nhãn VN | Màu/icon |
|---|---|---|
| `JobReceived` | Nhận việc | xám ⚪ |
| `PickStarted` | Bắt đầu gắp | xanh dương 🔵 |
| `PickCompleted` | Gắp xong | xanh dương 🔵 |
| `PlaceCompleted` | Đặt lên khay | xanh lá 🟢 |
| `Recovered` | Đã khắc phục | xanh lá 🟢 |
| `Error` | Lỗi | đỏ 🔴 (hiện `message`) |
| `Connected` / `Disconnected` | Tay online / offline | xám (mức trạm) |
| `EmergencyStop` | Dừng khẩn | đỏ 🔴 |

**Trạng thái rỗng:** `count == 0` → "Chưa có sự kiện nào cho đơn này" (đơn chưa vào phục vụ, hoặc executor chưa report). KHÁC hẳn dialog cũ trống-oan: giờ đã gom mọi job nên rỗng = **thật sự chưa có log**.

### Lọc theo loại — `?type=`
```
GET /api/manager/orders/{orderId}/events?type=Error          # chỉ lỗi
GET /api/manager/orders/{orderId}/events?type=PlaceCompleted # chỉ "đã đặt xong"
```
`type` không hợp lệ → BE bỏ lọc (trả tất cả), không lỗi. Gợi ý FE: 1 dropdown "Loại sự kiện" ngay trên timeline.

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
