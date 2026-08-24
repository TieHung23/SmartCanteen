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
- `currentUnitPrice`
- `expiresAtUtc`
- `isExpired`

`currentUnitPrice` is what the customer was charged for this order line, and it is the number a
replacement dish has to match. **Filter the candidate dishes against this, not against the current
menu price of `currentDishId`** — the two normally agree, but a menu price edited after the order was
placed would make the menu price the wrong one to compare with. It is `null` once the order line no
longer carries `currentDishId` (the proposal was already swapped or refunded), where it is no longer
needed. `GET /api/changeproposals` returns the same field per proposal.

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

Rules on the replacement dish:

- It must belong to the required category when `isRequiredItem = true`, otherwise to the same category as the dish being replaced.
- It must be active and part of the session.
- **It must cost exactly what the customer was charged for this item** (`currentUnitPrice` above). The wallet is debited in full when the order is placed and no step in this flow settles a difference, so a swap has to be money-neutral. The comparison is against the order item's charged `unitPrice`, not the current menu price of the dish being replaced - if the dish's price was edited after the order was placed, the two differ and the swap is rejected.
- **It must still have portions left in this session.** Available portions are derived as `preparedQuantity` of that dish minus the quantity the session's live orders still have to be served (lines that are `Pending`, `Confirmed` or `Swapped`; refunded, refund-pending and change-pending lines do not hold a portion, and cancelled/expired orders are excluded). A dish the manager never entered a prepared quantity for counts as zero and can never be swapped onto. Because an accepted swap flips the line to `Swapped` on the new dish, each committed swap is immediately visible to the next customer - the suggestion a proposal carries is a hint, and the first customers to answer take the spare portions.
- The resulting order must still satisfy its meal template (category min/max quantities).

When no equal-priced dish is acceptable to the customer, the swap is a dead end by design and the remaining options are the refund endpoints below: item refund for an optional item, full order refund for a required one.

Failure (`400`):

```json
{
  "message": "Replacement dish must cost the same as the item being replaced. Request an item refund or a full order refund instead.",
  "isSuccess": false
}
```

For a required item the same rejection reads `... Request a full order refund instead.`

Running out of portions is rejected the same way:

```json
{
  "message": "Replacement dish has no portions left in this session. Pick another dish or request a refund.",
  "errorCode": "InvalidValue",
  "isSuccess": false
}
```

The proposal stays `WaitingResponse` on both rejections, so the customer can pick a different dish or
switch to a refund - nothing is consumed by a failed attempt. Two customers racing for the last
portion can still both get through in the same instant: the endpoint opens no transaction and takes
no lock on the proposal.

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
