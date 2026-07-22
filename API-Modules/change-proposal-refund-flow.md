# Change Proposal Refund Flow API

Base URL: `/api`  
API Version: `1.0`  
Authentication: JWT Bearer Token  
Currency: **Point**

This document is for FE integration of the shortage resolution flow after a manager finalizes a session.

When the canteen cannot prepare enough quantity for an ordered dish, the backend creates a `ChangeProposal`. The customer must choose one of the allowed actions:

- Swap the missing dish to another valid dish.
- Request an item refund, only for optional items.
- Request a full order refund.

---

## 1. Business Rules

### Required Item Missing

Required item means the dish belongs to a required meal-template category.

Allowed customer actions:

```text
SwapItem
RefundOrder
```

Not allowed:

```text
RefundItem
```

If customer tries item refund on a required item, backend returns `400`.

### Optional Item Missing

Optional item means the dish does not belong to a required meal-template category.

Allowed customer actions:

```text
SwapItem
RefundItem
RefundOrder
```

---

## 2. Status Enums

### ChangeProposalStatus

| Value | Name | FE Meaning |
|-------|------|------------|
| `0` | `WaitingResponse` | Customer still needs to choose an action |
| `1` | `Accepted` | Customer accepted a swap dish |
| `2` | `RefundRequested` | Customer requested item-level refund |
| `3` | `OrderRefundRequested` | Customer requested full order refund |

### OrderItemStatus

| Value | Name | FE Meaning |
|-------|------|------------|
| `0` | `Pending` | Item is ordered, not finalized yet |
| `1` | `Confirmed` | Item is confirmed by manager/finalization |
| `2` | `ChangePending` | Item is missing and waiting for customer decision |
| `3` | `Swapped` | Item was swapped to another dish |
| `4` | `Refunded` | Item refund was approved |
| `5` | `RefundPending` | Item refund request is waiting for manager approval |

### RefundRequestStatus

| Value | Name | FE Meaning |
|-------|------|------------|
| `1` | `Pending` | Waiting for manager review |
| `2` | `Approved` | Approved and wallet credited |
| `3` | `Rejected` | Rejected by manager |

---

## 3. Required Backend Configuration

Proposal refund policies are configured in `Settings`.

### Full Order Refund From Proposal

| Group | Scope | Code | Example Value |
|-------|-------|------|---------------|
| `CHANGE_PROPOSAL` | `REFUND` | `ORDER_REFUND_POLICY_CODE` | `FULL_REFUND_NO_IMAGE` |

### Item Refund From Proposal

| Group | Scope | Code | Example Value |
|-------|-------|------|---------------|
| `CHANGE_PROPOSAL` | `REFUND` | `ITEM_REFUND_POLICY_CODE` | `PROPOSAL_ITEM_REFUND_NO_IMAGE` |

The referenced refund policy must exist under `REFUND_POLICY` and must not require images for proposal refund flows.

---

## 4. Main FE Flow

### Step 1: Manager Finalizes Session

Role: `Manager`  
Endpoint:

```http
POST /api/Sessions/{sessionId}/finalize
```

Request:

```json
{
  "preparedDishes": [
    {
      "dishId": "2f3c2bf7-150b-4308-8480-579354e9a0ac",
      "preparedQuantity": 0,
      "suggestedDishId": "e1d4e9b4-333d-4192-8e15-0a357f7de2da"
    },
    {
      "dishId": "9b4a0001-8cbe-4d41-801d-e03c0c1bedcf",
      "preparedQuantity": 0,
      "suggestedDishId": "cfe2703d-ad08-4f70-812f-aeedb105c2d1"
    }
  ]
}
```

Behavior:

- Dish with enough prepared quantity becomes confirmed.
- Dish with shortage creates `ChangeProposal`.
- Affected `OrderItem` becomes `ChangePending = 2`.
- Customer receives notification.

FE action:

- After notification or after session/order refresh, call `GET /api/ChangeProposals`.

---

### Step 2: Customer Gets Change Proposals

Role: authenticated customer  
Endpoint:

```http
GET /api/ChangeProposals
```

Response:

```json
{
  "value": [
    {
      "id": "6465e356-3c1b-491d-9ab2-6fc9a0f28678",
      "orderId": "ad55e9d5-9c30-41a5-ad9f-b288f2656587",
      "currentDishId": "2f3c2bf7-150b-4308-8480-579354e9a0ac",
      "currentDishName": "Bo luc lac",
      "suggestedDishId": "e1d4e9b4-333d-4192-8e15-0a357f7de2da",
      "suggestedDishName": "Bo xao hanh tay",
      "selectedDishId": null,
      "selectedDishName": null,
      "isRequiredItem": true,
      "requiredCategoryId": "d88e1801-8855-405b-a87e-d7814aec5a9b",
      "proposalStatus": 0,
      "allowedActions": [
        "SwapItem",
        "RefundOrder"
      ],
      "respondedAtUtc": null,
      "createdAtUtc": "2026-07-22T07:14:28.777565+07:00"
    },
    {
      "id": "d7061144-3ca4-432b-8dcd-b195b090f6a0",
      "orderId": "ad55e9d5-9c30-41a5-ad9f-b288f2656587",
      "currentDishId": "9b4a0001-8cbe-4d41-801d-e03c0c1bedcf",
      "currentDishName": "Bun tuoi",
      "suggestedDishId": "cfe2703d-ad08-4f70-812f-aeedb105c2d1",
      "suggestedDishName": "Bap luoc",
      "selectedDishId": null,
      "selectedDishName": null,
      "isRequiredItem": false,
      "requiredCategoryId": null,
      "proposalStatus": 0,
      "allowedActions": [
        "SwapItem",
        "RefundItem",
        "RefundOrder"
      ],
      "respondedAtUtc": null,
      "createdAtUtc": "2026-07-22T07:14:28.777565+07:00"
    }
  ],
  "message": "Change proposals retrieved successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": {}
}
```

FE usage:

- Use `id` as `proposalId`.
- Render actions from `allowedActions`, not from local hardcode.
- If `proposalStatus != 0`, disable action buttons.
- If `isRequiredItem = true`, do not show item refund button.

---

### Step 3: Customer Views One Proposal

Role: authenticated customer  
Endpoint:

```http
GET /api/ChangeProposals/{proposalId}
```

Response shape is the same as one item from `GET /api/ChangeProposals`.

Use this endpoint after an action to refresh one proposal.

---

## 5. Action: Swap Item

Role: authenticated customer  
Endpoint:

```http
POST /api/ChangeProposals/{proposalId}/accept
```

Request:

```json
{
  "proposalId": "6465e356-3c1b-491d-9ab2-6fc9a0f28678",
  "newDishId": "e1d4e9b4-333d-4192-8e15-0a357f7de2da"
}
```

Notes:

- `proposalId` in body is set by Swagger/client, but route id is the source used by controller.
- `newDishId` can be the suggested dish or another allowed dish.
- For required item, replacement must belong to the required category.
- For optional item, replacement must belong to the same category as the current dish.

Success response:

```json
{
  "value": {
    "message": "Dish swapped successfully."
  },
  "message": "Dish swapped successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": {}
}
```

State changes:

| Entity | Before | After |
|--------|--------|-------|
| `ChangeProposal.ProposalStatus` | `0 WaitingResponse` | `1 Accepted` |
| `OrderItem.ItemStatus` | `2 ChangePending` | `3 Swapped` |
| `OrderItem.DishId` | current dish | selected replacement dish |

FE refresh:

```http
GET /api/Orders/{orderId}
GET /api/ChangeProposals/{proposalId}
```

Expected order item:

```json
{
  "dishId": "e1d4e9b4-333d-4192-8e15-0a357f7de2da",
  "dishName": "Bo xao hanh tay",
  "quantity": 1,
  "unitPrice": 15,
  "itemStatus": 3
}
```

Common errors:

| HTTP | Message | FE Handling |
|------|---------|-------------|
| `400` | `Cannot accept proposal in status ...` | Refresh proposal; action was already taken |
| `400` | selected category exceeds maximum quantity | Ask user to choose another dish |
| `400` | selected dish is invalid for required/optional category | Show validation message |

---

## 6. Action: Item Refund From Proposal

Role: authenticated customer  
Endpoint:

```http
POST /api/ChangeProposals/{proposalId}/request-refund
```

Request body:

```text
No body
```

Only allowed when:

```text
isRequiredItem = false
allowedActions contains "RefundItem"
proposalStatus = 0
```

Success response:

```json
{
  "value": {
    "refundRequestId": "7a991b68-8286-4376-8004-b66bc9ddd23a",
    "orderId": "ad55e9d5-9c30-41a5-ad9f-b288f2656587",
    "orderItemId": 355,
    "dishId": "9b4a0001-8cbe-4d41-801d-e03c0c1bedcf",
    "policyCode": "PROPOSAL_ITEM_REFUND_NO_IMAGE",
    "refundAmount": 3,
    "status": "Pending",
    "message": "Item refund request submitted successfully."
  },
  "message": "Item refund request submitted successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": {}
}
```

Backend amount rule:

```text
refundAmount = OrderItem.UnitPrice.Amount * OrderItem.Quantity * policyPercent / 100
```

For `PROPOSAL_ITEM_REFUND_NO_IMAGE`, current policy is `100%`, so amount equals item total.

State changes:

| Entity | Before | After |
|--------|--------|-------|
| `ChangeProposal.ProposalStatus` | `0 WaitingResponse` | `2 RefundRequested` |
| `OrderItem.ItemStatus` | `2 ChangePending` | `5 RefundPending` |
| `RefundRequest.Status` | none | `1 Pending` |

FE refresh:

```http
GET /api/Orders/{orderId}
GET /api/refunds/{refundRequestId}
GET /api/ChangeProposals/{proposalId}
```

Expected order item while waiting manager approval:

```json
{
  "dishId": "9b4a0001-8cbe-4d41-801d-e03c0c1bedcf",
  "dishName": "Bun tuoi",
  "quantity": 1,
  "unitPrice": 3,
  "itemStatus": 5
}
```

Common errors:

| HTTP | Message | FE Handling |
|------|---------|-------------|
| `400` | `Required item cannot be refunded separately...` | Hide item refund for required item |
| `400` | `This order item already has a pending or approved refund request.` | Refresh refund/order state |
| `400` | `Configured change proposal item refund policy is not active or invalid.` | Show generic config error; ask admin/manager |
| `400` | `Cannot request refund on proposal in status ...` | Refresh proposal; action already taken |

---

## 7. Action: Full Order Refund From Proposal

Role: authenticated customer  
Endpoint:

```http
POST /api/ChangeProposals/{proposalId}/request-order-refund
```

Request body:

```text
No body
```

Allowed for:

- Required item proposal.
- Optional item proposal.

Success response:

```json
{
  "value": {
    "refundRequestId": "32995f2d-2cda-4297-9a90-10f13ed961f8",
    "orderId": "b6fc2784-3653-418d-9afb-92479436ed88",
    "policyCode": "FULL_REFUND_NO_IMAGE",
    "refundAmount": 15,
    "status": "Pending",
    "message": "Full order refund request submitted successfully."
  },
  "message": "Full order refund request submitted successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": {}
}
```

State changes:

| Entity | Before | After |
|--------|--------|-------|
| `ChangeProposal.ProposalStatus` | `0 WaitingResponse` | `3 OrderRefundRequested` |
| `Order.Status` | active/paid state | `3 Cancelled` |
| `RefundRequest.Status` | none | `1 Pending` |

RefundRequest context:

```json
{
  "orderItemId": null,
  "changeProposalId": "proposalId",
  "dishId": "currentDishId"
}
```

FE behavior:

- After success, navigate user to order detail or refund detail.
- Disable all pending proposal actions for the cancelled order.
- Show that wallet credit is still waiting manager approval until refund status becomes `Approved`.

---

## 8. Manager Refund Review APIs

Base route:

```http
/api/manager/refunds
```

Role:

```text
Manager
```

### List Refund Requests

```http
GET /api/manager/refunds?pageNumber=1&pageSize=10&status=1
```

`status` is optional.

Response item includes proposal context:

```json
{
  "id": "7a991b68-8286-4376-8004-b66bc9ddd23a",
  "orderId": "ad55e9d5-9c30-41a5-ad9f-b288f2656587",
  "orderItemId": 355,
  "changeProposalId": "d7061144-3ca4-432b-8dcd-b195b090f6a0",
  "dishId": "9b4a0001-8cbe-4d41-801d-e03c0c1bedcf",
  "userId": "5281a07e-d9aa-4764-892f-b8efaef332f4",
  "userName": "NGUYEN THI KIM PHUNG",
  "studentId": "Se184112",
  "policyName": "Change proposal item refund without image",
  "refundPercent": 100,
  "orderAmount": 3,
  "refundAmount": 3,
  "status": "Pending",
  "imageCount": 0,
  "createdAtUtc": "2026-07-22T07:20:26.2942267+07:00",
  "reviewedAtUtc": null
}
```

### Get Refund Detail

```http
GET /api/manager/refunds/{refundRequestId}
```

Use this before approve/reject if FE needs to confirm:

- Customer.
- Amount.
- Policy.
- Order item context.
- Proposal context.

Important fields:

```json
{
  "id": "7a991b68-8286-4376-8004-b66bc9ddd23a",
  "orderId": "ad55e9d5-9c30-41a5-ad9f-b288f2656587",
  "orderItemId": 355,
  "changeProposalId": "d7061144-3ca4-432b-8dcd-b195b090f6a0",
  "dishId": "9b4a0001-8cbe-4d41-801d-e03c0c1bedcf",
  "policyCode": "PROPOSAL_ITEM_REFUND_NO_IMAGE",
  "refundAmount": 3,
  "status": "Pending"
}
```

### Approve Refund

```http
POST /api/manager/refunds/{refundRequestId}/approve
```

Request body:

```text
No body
```

Success response:

```json
{
  "value": {
    "id": "7a991b68-8286-4376-8004-b66bc9ddd23a",
    "walletTransactionId": "0a97713f-233b-44e8-8e6b-b6e9e0e85a2a",
    "refundAmount": 3,
    "balanceAfter": 1003,
    "status": "Approved"
  },
  "message": "Refund request approved and wallet credited successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": {}
}
```

State changes for item refund:

| Entity | Before | After |
|--------|--------|-------|
| `RefundRequest.Status` | `1 Pending` | `2 Approved` |
| `OrderItem.ItemStatus` | `5 RefundPending` | `4 Refunded` |
| Customer wallet | old balance | old balance + refund amount |

State changes for full order refund:

| Entity | Before | After |
|--------|--------|-------|
| `RefundRequest.Status` | `1 Pending` | `2 Approved` |
| Customer wallet | old balance | old balance + refund amount |

Common errors:

| HTTP | Message | FE Handling |
|------|---------|-------------|
| `400` | `Refund request is no longer pending.` | Refresh refund detail; another action already happened |
| `400` | `Refund request order item not found.` | Show data error; refresh order/refund |

### Reject Refund

```http
POST /api/manager/refunds/{refundRequestId}/reject
```

Request:

```json
{
  "reason": "Reject item refund test"
}
```

Success response:

```json
{
  "value": {
    "id": "7a991b68-8286-4376-8004-b66bc9ddd23a",
    "status": "Rejected",
    "rejectionReason": "Reject item refund test"
  },
  "message": "Refund request rejected successfully.",
  "isSuccess": true,
  "isFailure": false,
  "error": {}
}
```

State changes for item refund:

| Entity | Before | After |
|--------|--------|-------|
| `RefundRequest.Status` | `1 Pending` | `3 Rejected` |
| `OrderItem.ItemStatus` | `5 RefundPending` | `2 ChangePending` |
| `ChangeProposal.ProposalStatus` | `2 RefundRequested` | `0 WaitingResponse` |

FE behavior after item refund reject:

- Refresh proposal detail.
- Show available actions again.
- Customer can choose swap item or full order refund.

---

## 9. Customer Refund Read APIs

### List Current User Refunds

```http
GET /api/refunds?pageNumber=1&pageSize=10&status=1
```

Response item includes proposal context:

```json
{
  "id": "7a991b68-8286-4376-8004-b66bc9ddd23a",
  "orderId": "ad55e9d5-9c30-41a5-ad9f-b288f2656587",
  "orderItemId": 355,
  "changeProposalId": "d7061144-3ca4-432b-8dcd-b195b090f6a0",
  "dishId": "9b4a0001-8cbe-4d41-801d-e03c0c1bedcf",
  "policyName": "Change proposal item refund without image",
  "refundPercent": 100,
  "orderAmount": 3,
  "refundAmount": 3,
  "status": "Pending",
  "imageCount": 0,
  "createdAtUtc": "2026-07-22T07:20:26.2942267+07:00",
  "reviewedAtUtc": null
}
```

### Get Current User Refund Detail

```http
GET /api/refunds/{refundRequestId}
```

Use this to show:

- Pending approval.
- Approved and credited.
- Rejected reason.

---

## 10. Order Detail Fields FE Must Watch

Endpoint:

```http
GET /api/Orders/{orderId}
```

Relevant response fragment:

```json
{
  "value": {
    "id": "ad55e9d5-9c30-41a5-ad9f-b288f2656587",
    "status": 4,
    "totalPrice": 18,
    "items": [
      {
        "dishId": "9b4a0001-8cbe-4d41-801d-e03c0c1bedcf",
        "dishName": "Bun tuoi",
        "quantity": 1,
        "unitPrice": 3,
        "itemStatus": 5
      },
      {
        "dishId": "e1d4e9b4-333d-4192-8e15-0a357f7de2da",
        "dishName": "Bo xao hanh tay",
        "quantity": 1,
        "unitPrice": 15,
        "itemStatus": 3
      }
    ]
  }
}
```

Important:

- `Order.status` is order-level status.
- `items[].itemStatus` is item-level status.
- Do not use `Order.status` to decide whether an individual item was refunded.

---

## 11. Notification Events FE Should Refresh On

The app sends notifications. FE should refresh related screens when these events arrive:

| Notification | Suggested FE Refresh |
|--------------|----------------------|
| Change proposal created | `GET /api/ChangeProposals`, `GET /api/Orders/{orderId}` |
| Item refund requested | Manager refreshes `GET /api/manager/refunds` |
| Full order refund requested | Manager refreshes `GET /api/manager/refunds` and order list |
| Refund approved | Customer refreshes `GET /api/refunds/{id}`, `GET /api/Orders/{orderId}`, wallet balance |
| Refund rejected | Customer refreshes `GET /api/refunds/{id}`, `GET /api/ChangeProposals/{proposalId}`, `GET /api/Orders/{orderId}` |

---

## 12. FE Decision Table

### Proposal Card Buttons

| Condition | Show Swap | Show Item Refund | Show Full Order Refund |
|-----------|-----------|------------------|------------------------|
| `proposalStatus != 0` | No | No | No |
| `isRequiredItem = true` | Yes | No | Yes |
| `isRequiredItem = false` | Yes | Yes | Yes |

Recommended implementation:

- Prefer `allowedActions` from API.
- Treat enum/status tables as display helpers only.
- Disable submit button while request is in flight.
- After success, refresh proposal and order detail.

### Item Status Display

| `itemStatus` | Suggested Label |
|--------------|-----------------|
| `0` | Pending |
| `1` | Confirmed |
| `2` | Needs response |
| `3` | Swapped |
| `4` | Refunded |
| `5` | Refund pending |

### Refund Status Display

| `status` | Suggested Label |
|----------|-----------------|
| `Pending` | Waiting for manager approval |
| `Approved` | Refunded to wallet |
| `Rejected` | Refund rejected |

---

## 13. End-to-End Test Checklist

### Required Item Missing

1. Manager finalizes session with required dish shortage.
2. Customer sees proposal:
   - `isRequiredItem = true`
   - `allowedActions = ["SwapItem", "RefundOrder"]`
3. Customer tries item refund.
   - Expected: `400`
4. Customer accepts swap.
   - Expected proposal: `proposalStatus = 1`
   - Expected order item: `itemStatus = 3`

### Optional Item Refund Approved

1. Manager finalizes session with optional dish shortage.
2. Customer sees proposal:
   - `isRequiredItem = false`
   - `allowedActions` contains `RefundItem`
3. Customer calls item refund.
   - Expected refund request: `status = Pending`
   - Expected order item: `itemStatus = 5`
4. Manager approves refund.
   - Expected refund request: `status = Approved`
   - Expected order item: `itemStatus = 4`
   - Expected wallet credited.

### Optional Item Refund Rejected

1. Create another order/session test.
2. Customer calls item refund.
3. Manager rejects refund.
   - Expected refund request: `status = Rejected`
   - Expected order item: `itemStatus = 2`
   - Expected proposal: `proposalStatus = 0`
4. Customer can choose swap or full order refund again.

### Full Order Refund From Proposal

1. Customer calls full order refund.
2. Expected refund request:
   - `orderItemId = null`
   - `changeProposalId = proposalId`
   - `dishId = currentDishId`
   - `status = Pending`
3. Expected order:
   - order status becomes `Cancelled`.
4. Manager approves refund.
   - Customer wallet is credited.
