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

**Refund is auto-approved and wallet-credited immediately** — no manager review step. This applies to every refund that originates from a `ChangeProposal` (the kitchen's fault, not a customer claim), as opposed to a manually-submitted refund with photo evidence (`POST /api/refunds`), which still requires manager approval.

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
    "status": "Approved",
    "message": "Item refund approved and credited automatically."
  },
  "isSuccess": true
}
```

State changes (all happen synchronously in this one request):

- `ChangeProposal.ProposalStatus = 2` (`RefundRequested`)
- `OrderItem.ItemStatus = 4` (`Refunded`) — goes straight to final state, no manager step
- `RefundRequest.Status = 2` (`Approved`), `reviewedBy = null` (auto-credited, not manager-reviewed)
- Customer wallet credited immediately; a `WalletTransaction` is created

## `POST /api/changeproposals/{id}/request-order-refund`

User requests a full order refund. The API creates a `RefundRequest` using the configured policy, **auto-approves and credits it immediately**, marks the proposal as `OrderRefundRequested`, and cancels the order.

Response:

```json
{
  "value": {
    "refundRequestId": "guid",
    "orderId": "guid",
    "policyCode": "FULL_REFUND_NO_IMAGE",
    "refundAmount": 100000,
    "status": "Approved",
    "message": "Full order refund approved and credited automatically."
  },
  "isSuccess": true
}
```

**Amount calculation:** `refundAmount` is computed on **items not already `Refunded`** — if another item in the same order was already refunded individually before this call, its price is excluded so the customer is never double-refunded. If the order has multiple items and none were refunded yet, `refundAmount` covers the **whole order**, including items unrelated to this proposal (choosing full-order refund cancels everything, not just the shortage item).

**Sibling cascade:** if the order has other proposals still `WaitingResponse`, they are automatically transitioned to `OrderRefundRequested` in the same call — no dangling proposals left on a cancelled order. Refresh `GET /api/ChangeProposals` after this call to see the updated state of all proposals in the order.

**Duplicate guard:** blocked with `400` only if the order **already has another full-order refund** (`orderItemId == null`) `Pending`/`Approved`. An existing item-level refund on a different item does **not** block this call anymore.

---

## Automatic Expiration

The backend creates each proposal with `expiresAtUtc = now + RESPONSE_WINDOW_MINUTES`.

A background job runs every minute and processes proposals where:

```text
proposalStatus = WaitingResponse
expiresAtUtc <= now
```

Resolution rules:

- Required affected item: create a full-order refund request (amount = remaining un-refunded items, same rule as the manual endpoint above), **auto-approve and credit it**, cancel the order, cascade-resolve sibling proposals in that order to `OrderRefundRequested`, and notify the customer.
- Optional affected item: create an item-level refund request, **auto-approve and credit it**, mark the item `Refunded`, mark the proposal `RefundRequested`, and notify the customer.

**Refund requests created this way are `Approved` immediately, not `Pending`** — the customer's wallet is credited without any manager action, same as the manual endpoints above. `reviewedBy` is `null` on these refunds.
