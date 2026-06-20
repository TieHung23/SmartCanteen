# Flow: Manager duyệt Verification Request

> Hướng dẫn FE tích hợp luồng Manager xem xét và duyệt/từ chối yêu cầu xác thực danh tính của User.

---

## 1. Tổng quan

User gửi yêu cầu xác thực danh tính (VerificationRequest) kèm ảnh giấy tờ. Manager xem xét và:
- **Approve:** Xác nhận user là sinh viên hợp lệ
- **Reject:** Từ chối kèm lý do

Khi được duyệt, user được gắn claim `verified=true` và có thể sử dụng các tính năng yêu cầu xác thực.

---

## 2. Sequence Diagram

```
User (FE)                    Manager (FE)                  Backend
    │                             │                            │
    │  Submit verification         │                            │
    │  POST /api/verification/     │                            │
    │  submit (multipart)          │                            │
    │─────────────────────────────>│                            │
    │                              │  Validate + upload to     │
    │                              │  Cloudinary + INSERT      │
    │                              │───────────────────────────>│
    │<─────────────────────────────│                            │
    │  201 Created                 │                            │
    │                              │                            │
    │                              │  1. List verifications     │
    │                              │  GET /api/admin/verif      │
    │                              │───────────────────────────>│
    │                              │<───────────────────────────│
    │                              │  Danh sách đang chờ       │
    │                              │                            │
    │                              │  2. Xem chi tiết          │
    │                              │  GET /api/admin/verif/{id} │
    │                              │───────────────────────────>│
    │                              │<───────────────────────────│
    │                              │  Thông tin + documents    │
    │                              │                            │
    │                              │  3a. Approve              │
    │                              │  POST .../approve         │
    │                              │───────────────────────────>│
    │                              │  Update User.EmailVerified│
    │                              │  + set claim              │
    │                              │<───────────────────────────│
    │                              │  200 OK                   │
    │                              │                            │
    │  (User nhận notification)    │                            │
    │<─────────────────────────────│                            │
    │                              │  3b. Reject               │
    │                              │  POST .../reject          │
    │                              │  { reason: "..." }        │
    │                              │───────────────────────────>│
    │                              │<───────────────────────────│
    │                              │  200 OK                   │
    │                              │                            │
    │  (User nhận notification)    │                            │
    │<─────────────────────────────│                            │
```

---

## 3. API Endpoints

### 3.1 List pending verifications

```
GET /api/admin/verifications
Auth: Manager
Query: ?pageNumber=1&pageSize=10
```

**Paginated response items:**
```json
{
  "items": [
    {
      "id": "guid",
      "userId": "guid",
      "userEmail": "sinhvien@example.com",
      "userName": "Nguyễn Văn A",
      "submittedAt": "2026-06-19T14:30:00Z",
      "documentCount": 2
    }
  ]
}
```

### 3.2 Get verification detail

```
GET /api/admin/verifications/{id}
Auth: Manager
```

**Response:**
```json
{
  "value": {
    "id": "guid",
    "userId": "guid",
    "userEmail": "sinhvien@example.com",
    "userName": "Nguyễn Văn A",
    "studentId": "SE123456",
    "majorOrClass": "Khoa học máy tính",
    "dateOfBirth": "2000-01-15",
    "status": 0,
    "submittedAt": "2026-06-19T14:30:00Z",
    "reviewedAt": null,
    "reviewedBy": null,
    "rejectionReason": null,
    "expiresAt": "2026-07-19T14:30:00Z",
    "documents": [
      {
        "id": "guid",
        "documentType": 0,
        "cloudinaryUrl": "https://res.cloudinary.com/...",
        "fileName": "the_sinh_vien.jpg",
        "fileSize": 245000,
        "mimeType": "image/jpeg",
        "uploadedAt": "2026-06-19T14:30:00Z"
      }
    ]
  },
  "isSuccess": true
}
```

### 3.3 Approve

```
POST /api/admin/verifications/{id}/approve
Auth: Manager
Request body: None
```

**Response:**
```json
{
  "value": {
    "id": "guid",
    "message": "Verification request approved successfully."
  },
  "isSuccess": true
}
```

### 3.4 Reject

```
POST /api/admin/verifications/{id}/reject
Auth: Manager
```

**Request body:**
```json
{
  "reason": "Hình ảnh không rõ ràng, vui lòng chụp lại."
}
```

**Response:**
```json
{
  "value": {
    "id": "guid",
    "message": "Verification request rejected."
  },
  "isSuccess": true
}
```

---

## 4. UI/UX gợi ý

### Màn hình danh sách

```
┌─── ✅ Duyệt xác thực danh tính ────────────────────────────────────────┐
│                                                                        │
│  Bộ lọc: [Tất cả ▼]  Tìm: [______________]                            │
│                                                                        │
│  ┌──────┬──────────────┬──────────────────┬────────────┬──────────────┐│
│  │      │ User         │ Email            │ Ngày gửi   │ Documents    ││
│  ├──────┼──────────────┼──────────────────┼────────────┼──────────────┤│
│  │ 🔴   │ Nguyễn Văn A │ sv1@example.com  │ 19/06/2026 │ 2 ảnh        ││
│  │ 🔴   │ Trần Thị B   │ sv2@example.com  │ 19/06/2026 │ 1 ảnh        ││
│  │ 🟢   │ Lê Văn C     │ sv3@example.com  │ 18/06/2026 │ 3 ảnh        ││
│  └──────┴──────────────┴──────────────────┴────────────┴──────────────┘│
│                                                          << < 1 > >>  │
│  🔴 = Chờ duyệt      🟢 = Đã duyệt       ⚫ = Từ chối                 │
└────────────────────────────────────────────────────────────────────────┘
```

### Màn hình chi tiết + action

Khi click vào một item đang chờ:

```
┌─── 📋 Chi tiết yêu cầu xác thực ─────────────────────────────────────┐
│                                                                       │
│  Thông tin user                                                       │
│  ─────────────────────────────────────────────────────────────        │
│  Họ tên:      Nguyễn Văn A                                           │
│  Email:       sinhvien@example.com                                    │
│  MSSV:        SE123456                                                │
│  Lớp:         Khoa học máy tính                                       │
│  Ngày sinh:   15/01/2000                                              │
│  Gửi lúc:     19/06/2026 14:30                                        │
│  Hết hạn:     19/07/2026 14:30                                        │
│                                                                       │
│  Documents                                                            │
│  ─────────────────────────────────────────────────────────────        │
│  ┌────────────────────┐   ┌────────────────────┐                      │
│  │                    │   │                    │                      │
│  │  📷 Thẻ sinh viên  │   │  📷 CMND           │                      │
│  │                    │   │                    │                      │
│  │  [Xem toàn màn hình]│   │  [Xem toàn màn hình]│                    │
│  └────────────────────┘   └────────────────────┘                      │
│                                                                       │
│  ─────────────────────────────────────────────────────────────        │
│                                                                       │
│  [✅ Duyệt]                    [❌ Từ chối]                           │
│                                                                       │
│  (Khi click Từ chối → hiện modal nhập lý do)                         │
└───────────────────────────────────────────────────────────────────────┘
```

### Modal từ chối

```
┌─── Từ chối xác thực ─────────────────────────────────┐
│                                                       │
│  Lý do từ chối:                                       │
│  ┌─────────────────────────────────────────────────┐ │
│  │ Hình ảnh không rõ ràng, vui lòng chụp lại.      │ │
│  └─────────────────────────────────────────────────┘ │
│                                                       │
│  [Hủy]                            [Xác nhận từ chối] │
└───────────────────────────────────────────────────────┘
```

### Modal xác nhận duyệt

```
┌─── Xác nhận duyệt ────────────────────────────────────┐
│                                                       │
│  ✅ Duyệt yêu cầu xác thực của Nguyễn Văn A?          │
│                                                       │
│  User sẽ được gắn trạng thái "Đã xác thực"            │
│                                                       │
│  [Hủy]                              [Xác nhận duyệt] │
└───────────────────────────────────────────────────────┘
```

---

## 5. Document Types

| Value | Type | Mô tả |
|-------|------|-------|
| 0 | `StudentCard` | Thẻ sinh viên |
| 1 | `Transcript` | Bảng điểm |
| 2 | `Other` | Giấy tờ khác |

---

## 6. Verification Status

| Value | Status | Màu sắc |
|-------|--------|---------|
| 0 | `Pending` | 🔴 Đỏ (chờ duyệt) |
| 1 | `Approved` | 🟢 Xanh (đã duyệt) |
| 2 | `Rejected` | ⚫ Đen (từ chối) |
| 3 | `Expired` | ⚪ Xám (hết hạn) |

---

## 7. Notification cho User

Khi manager duyệt hoặc từ chối, user nhận notification:

**Payload approve:**
```json
{
  "type": "verification_approved",
  "title": "Xác thực thành công",
  "message": "Yêu cầu xác thực danh tính của bạn đã được duyệt.",
  "referenceType": "VerificationRequest",
  "referenceId": "guid"
}
```

**Payload reject:**
```json
{
  "type": "verification_rejected",
  "title": "Xác thực bị từ chối",
  "message": "Lý do: Hình ảnh không rõ ràng, vui lòng chụp lại.",
  "referenceType": "VerificationRequest",
  "referenceId": "guid"
}
```

---

## 8. FE integration checklist

| Bước | Mô tả | API |
|------|-------|-----|
| 1 | Hiển thị badge/icon trạng thái verification trên profile user | Thông tin có trong `GET /api/verification/me` |
| 2 | Manager list tất cả yêu cầu | `GET /api/admin/verifications` |
| 3 | Manager xem chi tiết + ảnh documents | `GET /api/admin/verifications/{id}` |
| 4 | Manager duyệt | `POST .../approve` |
| 5 | Manager từ chối kèm lý do | `POST .../reject` |
| 6 | User nhận notification kết quả | SignalR event `ReceiveNotification` |
