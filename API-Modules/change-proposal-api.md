# SmartCanteen Change Proposal API

Base URL: `/api/changeproposals`
Auth: `[Authorize]`
API Version: `1.0`

Proposals are created automatically by `POST /api/sessions/{id}/finalize` when a dish is under-supplied. Users can swap the item, request an item refund when allowed, or request a full order refund.

---

## Required Configuration

Proposal refund reads refund policy codes from `Settings`:

| Group | Scope | Code | Value |
|-------|-------|------|-------|
| `CHANGE_PROPOSAL` | `REFUND` | `ORDER_REFUND_POLICY_CODE` | Refund policy code, for example `FULL_REFUND_NO_IMAGE` |
| `CHANGE_PROPOSAL` | `REFUND` | `ITEM_REFUND_POLICY_CODE` | Refund policy code, for example `PROPOSAL_ITEM_REFUND_NO_IMAGE` |
| `CHANGE_PROPOSAL` | `RESPONSE` | `RESPONSE_WINDOW_MINUTES` | Minutes before an unanswered proposal is automatically resolved. Default seed: `30` |

The referenced refund policy must exist under `REFUND_POLICY` and must not require images.

## `GET /api/changeproposals`

Returns current user's change proposals.

## `GET /api/changeproposals/{id}`

Returns proposal detail, including:

- `isRequiredItem`
- `requiredCategoryId`
- `allowedActions`
- `suggestedDishId`
- `selectedDishId`
- `expiresAtUtc`
- `isExpired`

When `isExpired = true`, `allowedActions` is empty and action APIs return `400`.

Required item proposals allow:

```json
["SwapItem", "RefundOrder"]
```

Optional item proposals allow:

```json
["SwapItem", "RefundItem", "RefundOrder"]
```

## `POST /api/changeproposals/{id}/accept`

User accepts a replacement dish.

Request:

```json
{
  "newDishId": "guid"
}
```

If `isRequiredItem = true`, the replacement dish must belong to the required category.

Response:

```json
{
  "value": { "message": "Dish swapped successfully." },
  "isSuccess": true
}
```

## `POST /api/changeproposals/{id}/request-refund`

User requests an item-level refund. This is allowed only when `isRequiredItem = false`.

Response:

```json
{
  "value": {
    "refundRequestId": "guid",
    "orderId": "guid",
    "orderItemId": 355,
    "dishId": "guid",
    "policyCode": "PROPOSAL_ITEM_REFUND_NO_IMAGE",
    "refundAmount": 3,
    "status": "Pending",
    "message": "Item refund request submitted successfully."
  },
  "isSuccess": true
}
```

State changes:

- `ChangeProposal.ProposalStatus = 2` (`RefundRequested`)
- `OrderItem.ItemStatus = 5` (`RefundPending`)
- Manager approval changes item status to `4` (`Refunded`)

## `POST /api/changeproposals/{id}/request-order-refund`

User requests a full order refund. The API creates a `RefundRequest` using the configured policy, marks the proposal as `OrderRefundRequested`, and cancels the order.

Response:

```json
{
  "value": {
    "refundRequestId": "guid",
    "orderId": "guid",
    "policyCode": "FULL_REFUND_NO_IMAGE",
    "refundAmount": 100000,
    "status": "Pending",
    "message": "Full order refund request submitted successfully."
  },
  "isSuccess": true
}
```

---

## Automatic Expiration

The backend creates each proposal with `expiresAtUtc = now + RESPONSE_WINDOW_MINUTES`.

A background job runs every minute and processes proposals where:

```text
proposalStatus = WaitingResponse
expiresAtUtc <= now
```

Resolution rules:

- Required affected item: create a full-order refund request, cancel the order, mark waiting proposals in that order as `OrderRefundRequested`, and notify the customer.
- Optional affected item: create an item-level refund request, mark the item as `RefundPending`, mark the proposal as `RefundRequested`, and notify the customer.

Refund requests are still created as `Pending`; manager approval follows the normal refund workflow.
