# Flow: Manager tạo Session

> Hướng dẫn FE tích hợp luồng Manager tạo phiên ăn mới.

---

## 1. Tổng quan

Manager tạo một session (phiên ăn) gồm:
- Thông tin cơ bản: tên, mô tả, thời gian
- Danh sách món ăn (Dish) kèm số lượng dự kiến
- Các MealTemplate (mẫu suất ăn) với cấu hình danh mục

---

## 2. Sequence Diagram

```
Manager (FE)                    Backend                      DB
    │                               │                        │
    │  1. Fetch dishes               │                        │
    │  GET /api/dishes               │                        │
    │───────────────────────────────>│                        │
    │<───────────────────────────────│                        │
    │  Danh sách món + categories    │                        │
    │                               │                        │
    │  2. Fetch categories           │                        │
    │  GET /api/categories           │                        │
    │───────────────────────────────>│                        │
    │<───────────────────────────────│                        │
    │                               │                        │
    │  3. Manager nhập form          │                        │
    │  ┌──────────────────────┐    │                        │
    │  │ Tên: "Buổi trưa T2"  │    │                        │
    │  │ Giờ mở: 10:00       │    │                        │
    │  │ Giờ đóng: 13:00     │    │                        │
    │  │ Món: Cơm gà x 50    │    │                        │
    │  │       Cơm sườn x 30 │    │                        │
    │  │ Template: Mặc định   │    │                        │
    │  └──────────────────────┘    │                        │
    │                               │                        │
    │  4. Tạo session                │                        │
    │  POST /api/sessions            │                        │
    │───────────────────────────────>│                        │
    │                               │                        │
    │                               │  Validate + INSERT     │
    │                               │───────────────────────>│
    │                               │<───────────────────────│
    │<───────────────────────────────│                        │
    │  201 Created + session info    │                        │
```

---

## 3. API Calls

### 3.1 Fetch dishes

```
GET /api/dishes?isActive=true
Auth: Authorize
```

**Response items:**
```json
{
  "items": [
    {
      "id": "guid",
      "name": "Cơm gà",
      "description": "Cơm trắng + gà kho",
      "priceAmount": 25.0,
      "priceCurrency": "Point",
      "isActive": true,
      "imgUrl": "https://res.cloudinary.com/...",
      "categoryId": "guid",
      "categoryName": "Món chính"
    }
  ]
}
```

### 3.2 Fetch categories

```
GET /api/categories
Auth: Authorize
```

**Response items:**
```json
{
  "items": [
    {
      "id": "guid",
      "name": "Món chính",
      "description": "Các món ăn chính"
    }
  ]
}
```

### 3.3 Create session

```
POST /api/sessions
Auth: Authorize (Manager)
```

**Request body:**
```json
{
  "name": "Buổi trưa thứ 2",
  "description": "Phục vụ sinh viên IT",
  "availableFrom": "2026-07-01T10:00:00+07:00",
  "availableTo": "2026-07-01T13:00:00+07:00",
  "availableForOrder": "2026-06-30T22:00:00+07:00",
  "finalizationDeadline": "2026-07-01T09:30:00+07:00",
  "autoFinalizePolicy": 0,
  "mealTemplates": [
    {
      "name": "Suất chuẩn",
      "settings": [
        { "categoryId": "guid-món-chính", "minQuantity": 1, "maxQuantity": 1, "isRequired": true },
        { "categoryId": "guid-món-phụ", "minQuantity": 0, "maxQuantity": 2, "isRequired": false },
        { "categoryId": "guid-canhh", "minQuantity": 0, "maxQuantity": 1, "isRequired": false }
      ]
    }
  ],
  "dishes": [
    { "dishId": "guid-cơm-gà", "quantity": 50 },
    { "dishId": "guid-cơm-sườn", "quantity": 30 },
    { "dishId": "guid-canh-chua", "quantity": 20 }
  ]
}
```

**Response 201:**
```json
{
  "value": {
    "id": "guid",
    "name": "Buổi trưa thứ 2",
    "message": "Session created successfully."
  },
  "isSuccess": true,
  "message": "Session created successfully."
}
```

---

## 4. UI/UX gợi ý

### Step 1: Chọn món cho session

```
┌─── Thêm món vào phiên ──────────────────────────────────┐
│  Tìm món: [________________]                             │
│                                                         │
│  Danh sách món (từ /api/dishes)                         │
│  ┌────────────────────────────────────────────────┐    │
│  │ ☐ Cơm gà     25 Point  │ SL dự kiến: [50]    │    │
│  │ ☐ Cơm sườn   30 Point  │ SL dự kiến: [30]    │    │
│  │ ☐ Canh chua  10 Point  │ SL dự kiến: [20]    │    │
│  │ ☐ Rau muống  15 Point  │ SL dự kiến: [__]    │    │
│  └────────────────────────────────────────────────┘    │
│                                                         │
│  [Thêm món đã chọn vào session]                         │
└─────────────────────────────────────────────────────────┘
```

### Step 2: Cấu hình thông tin cơ bản

```
┌─── Tạo phiên ăn mới ────────────────────────────────────┐
│                                                         │
│  Tên phiên:    [Buổi trưa thứ 2            ]            │
│  Mô tả:        [Phục vụ sinh viên IT       ]            │
│                                                         │
│  Giờ bắt đầu:  [01/07/2026 10:00]                       │
│  Giờ kết thúc: [01/07/2026 13:00]                       │
│  Giờ mở đặt:   [30/06/2026 22:00]                       │
│                                                         │
│  ⚙️ Cài đặt chốt đơn (tùy chọn):                        │
│  Hạn chốt:     [01/07/2026 09:30]                       │
│  Nếu quá hạn:  ▼ Hủy đơn (AutoReject)                   │
│                    Xác nhận tất cả (AutoConfirmAll)      │
│                                                         │
│  ──── Các món trong phiên ────                          │
│  🥘 Cơm gà                    SL dự kiến: 50            │
│  🥘 Cơm sườn                  SL dự kiến: 30            │
│  🥘 Canh chua                 SL dự kiến: 20            │
│                                                         │
│  ──── Mẫu suất ăn ────                                  │
│  [+ Thêm mẫu]                                            │
│  📋 Suất chuẩn:                                          │
│     Món chính: 1-1 (bắt buộc)                           │
│     Món phụ: 0-2 (không bắt buộc)                       │
│     Canh: 0-1 (không bắt buộc)                          │
│                                                         │
│  [Hủy]                              [Tạo phiên]         │
└─────────────────────────────────────────────────────────┘
```

### Step 3: Sau khi tạo thành công

- Chuyển hướng đến màn hình chi tiết session vừa tạo
- Hoặc hiển thị thông báo + nút "Xem session" / "Quản lý phiên"

```
┌─────────────────────────────────────────┐
│  ✅ Tạo phiên thành công!                │
│                                         │
│  Tên: Buổi trưa thứ 2                   │
│  Mã phiên: abc-def-123                  │
│  Trạng thái: Đang hoạt động             │
│                                         │
│  [Xem chi tiết]  [Quay lại danh sách]   │
└─────────────────────────────────────────┘
```

---

## 5. Validation notes

| Field | Validation |
|-------|-----------|
| `name` | Required, max 200 ký tự |
| `description` | Required, max 500 ký tự |
| `availableFrom` | Phải trước `availableTo` |
| `availableTo` | Phải sau `availableFrom` |
| `availableForOrder` | Phải trước `availableFrom` (cho phép user đặt trước) |
| `finalizationDeadline` | Optional; nếu có phải > thời điểm hiện tại |
| `dishes` | Phải có ít nhất 1 món |
| `dishId` | Phải tồn tại và đang active |
| `quantity` | > 0 |
| `mealTemplates[].settings` | `maxQuantity` >= `minQuantity`, `minQuantity` >= 0 |

---

## 6. Error handling

| Case | Status | Error |
|------|--------|-------|
| Dish not found or inactive | 400 | `NullValue` + dishId |
| Template name empty | 400 | `InvalidValue` |
| Invalid min/max quantity | 400 | `InvalidValue` + description |
| Server error | 500 | `ServerError` + message |
