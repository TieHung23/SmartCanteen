# Frontend Integration: Session Finalization & Change Proposals

> Hướng dẫn tích hợp tính năng Manager chốt đơn và user xử lý đề xuất đổi/refund món.

---

## 1. Luồng tổng quan

```
User đặt món tự do (không giới hạn số lượng)
      │
      ▼
Manager chốt số lượng (trước FinalizationDeadline)
      │
      ├── Món đủ PreparedQuantity ≥ ordered → Item.Confirmed ✅
      └── Món không đủ / không nấu → Item.ChangePending + Proposal (WaitingResponse)
              │
              ▼
          User nhận notification (SignalR)
              │
              ├── Chọn món khác → POST /api/changeproposals/{id}/accept
              │         → Item.Swapped, Proposal.Accepted
              │
              └── Yêu cầu refund → POST /api/changeproposals/{id}/request-refund
                        → Item.Refunded, Proposal.RefundRequested
                        → User tự submit RefundRequest (luồng cũ)
```

---

## 2. API endpoints mới

### 2.1 Manager: Finalize session

```
POST /api/sessions/{sessionId}/finalize
Auth: Manager
```

**Request:**
```json
{
  "preparedDishes": [
    { "dishId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "preparedQuantity": 50 },
    { "dishId": "6ba7b810-9dad-11d1-80b4-00c04fd430c8", "preparedQuantity": 30 }
  ]
}
```

Gửi danh sách tất cả dishId trong session kèm số lượng sẽ nấu (0 nếu không nấu món đó).

### 2.2 User: Accept change proposal

```
POST /api/changeproposals/{proposalId}/accept
Auth: User (must own proposal)
```

**Request:**
```json
{
  "newDishId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Lưu ý:** Frontend cần fetch danh sách món của session (khác với món hiện tại) để cho user chọn món thay thế.

### 2.3 User: Request refund from proposal

```
POST /api/changeproposals/{proposalId}/request-refund
Auth: User (must own proposal)
```

**Request body:** None (empty)

---

## 3. Thay đổi ở response hiện tại

### GET /api/orders/{id}

Mỗi item trong order response trả thêm `itemStatus`:

```json
{
  "items": [
    {
      "dishId": "guid",
      "quantity": 1,
      "unitPrice": 0.0,
      "itemStatus": 0
      // itemStatus: 0=Pending, 1=Confirmed, 2=ChangePending, 3=Swapped, 4=Refunded
    }
  ]
}
```

### GET /api/sessions/{id}

Mỗi dish trong session trả thêm `preparedQuantity`:

```json
{
  "dishes": [
    { "dishId": "guid", "quantity": 1, "preparedQuantity": null }
    // preparedQuantity: null = chưa chốt, số = số lượng sẽ nấu
  ]
}
```

Tương tự cho `GET /api/sessions` (list), dish cũng trả `preparedQuantity`.

---

## 4. SignalR Notification

Khi Manager finalize session và có proposal mới, user nhận realtime event qua SignalR hub:

**Event name:** (configured, mặc định `ReceiveNotification`)

**Payload khi có proposal mới:**
```json
{
  "id": "guid",
  "type": "change_proposal",
  "title": "Món đã chọn không còn khả dụng",
  "message": "Món 'Cơm gà' trong đơn #12345 không đủ số lượng. Vui lòng chọn món khác hoặc yêu cầu hoàn tiền.",
  "referenceType": "ChangeProposal",
  "referenceId": "proposal-guid",
  "actionUrl": "/orders/order-guid/change-proposal/proposal-guid",
  "metadata": {
    "orderId": "guid",
    "proposalId": "guid",
    "currentDishId": "guid",
    "currentDishName": "Cơm gà",
    "suggestedDishId": null
  },
  "createdAtUtc": "2024-01-01T00:00:00Z"
}
```

---

## 5. UI/UX gợi ý

### Màn hình Manager (chốt đơn)

Trong form edit session hoặc màn hình chi tiết session, thêm section:

```
┌─────────────────────────────────────────┐
│  📋 Chốt số lượng món cho phiên ăn      │
│  Deadline: 30/06/2026 10:00             │
│                                         │
│  Món                    SL đặt │ SL nấu │
│  ─────────────────────────────────────  │
│  🥘 Cơm gà                  45 │ [50]  │
│  🥘 Cơm sườn                30 │ [30]  │
│  🥘 Canh chua                20 │ [0]   │ ❗không nấu
│                                         │
│  [  Xác nhận chốt đơn  ]               │
│                                         │
│  ⚠ Còn 10s để chốt                     │
└─────────────────────────────────────────┘
```

- Ô "SL nấu" mặc định điền = SL đặt (có thể sửa)
- Nếu set = 0: món đó không được nấu, tất cả user đặt món này sẽ nhận proposal
- Nếu set < SL đặt nhưng > 0: user nào đặt món này cũng nhận proposal

### Màn hình User (xử lý proposal)

Popup hoặc page khi user nhận notification:

```
┌─────────────────────────────────────┐
│  ⚠ Món không đủ số lượng            │
│                                     │
│  Món "Cơm gà" trong đơn #12345     │
│  không đủ số lượng cho hôm nay.    │
│                                     │
│  Bạn muốn:                         │
│                                     │
│  [🔄 Đổi món khác]                  │
│     → Chọn từ danh sách món        │
│       (Cơm sườn, Cơm chiên, ...)   │
│                                     │
│  [💰 Hoàn tiền món này]             │
│     → Số tiền sẽ được hoàn vào ví   │
│                                     │
│  [❌ Bỏ qua] (nhắc sau)             │
└─────────────────────────────────────┘
```

### Badge trên OrderItem

Trong danh sách đơn hàng của user, mỗi item có thể hiển thị trạng thái:

| ItemStatus | Hiển thị |
|-----------|----------|
| 0 Pending | ✅ Đang xử lý |
| 1 Confirmed | ✅ Xác nhận |
| 2 ChangePending | ⚠️ Cần xử lý (màu vàng, kèm nút) |
| 3 Swapped | 🔄 Đã đổi món |
| 4 Refunded | 💰 Đã hoàn tiền |

---

## 6. Sequence cho FE

```
1. User vào trang order detail
   → GET /api/orders/{id}
   → Kiểm tra items[].itemStatus
   → Nếu có ChangePending → hiển thị warning + nút action

2. User nhận SignalR notification type "change_proposal"
   → Fetch proposal detail (hoặc đọc từ metadata)
   → Hiển thị popup/dialog

3. User chọn "Đổi món"
   → GET /api/sessions/{sessionId} (lấy danh sách món)
   → User chọn món thay thế
   → POST /api/changeproposals/{proposalId}/accept { newDishId }
   → Refresh order detail → itemStatus = 3 (Swapped)

4. User chọn "Hoàn tiền"
   → POST /api/changeproposals/{proposalId}/request-refund
   → Refresh order detail → itemStatus = 4 (Refunded)
   → User vào Refund page → submit RefundRequest (luồng cũ)
```

---

## 7. Session fields mới

Khi tạo/update session, có thể thêm `finalizationDeadline` và `autoFinalizePolicy`:

```json
{
  "name": "string",
  "description": "string",
  "availableFrom": "2024-01-01T00:00:00Z",
  "availableTo": "2024-01-01T00:00:00Z",
  "availableForOrder": "2024-01-01T00:00:00Z",
  "finalizationDeadline": "2024-01-01T09:30:00Z",
  "autoFinalizePolicy": 0,
  "mealTemplates": [],
  "dishes": []
}
```

| Field | Type | Description |
|-------|------|-------------|
| `finalizationDeadline` | DateTimeOffset (nullable) | Hạn chót Manager phải chốt đơn |
| `autoFinalizePolicy` | int | `0` = AutoReject (hủy hết), `1` = AutoConfirmAll (xác nhận tất cả, tạo proposals) |

Nếu không set deadline thì session không có finalization (giữ nguyên luồng cũ).
