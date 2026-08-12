# Frontend Integration: Session Finalization & Change Proposals

> Integration guide for the Manager order-finalization feature and how users handle dish swap/refund proposals.

---

## 1. Overall Flow

```
User orders freely (no quantity limit)
      │
      ▼
Manager finalizes quantities (before FinalizationDeadline)
      │
      ├── Category budget short → whole call rejected (HTTP 400), nothing saved ❌
      │     Σ PreparedQuantity of a category must cover Σ ordered of that category
      │
      ├── Dish has enough: PreparedQuantity ≥ ordered → Item.Confirmed ✅
      └── Dish short / not cooked → Item.ChangePending + Proposal (WaitingResponse)
              │     earliest orders keep the dish; the overflow is auto-matched to a
              │     category mate that still has spare PreparedQuantity
              │
              ▼
          User receives a notification (SignalR)
              │
              ├── Picks another dish → POST /api/changeproposals/{id}/accept
              │         → Item.Swapped, Proposal.Accepted
              │
              └── Requests a refund → POST /api/changeproposals/{id}/request-refund
                        → Item.Refunded, Proposal.RefundRequested
                        → User submits a RefundRequest themselves (existing flow)
```

---

## 2. New API endpoints

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

Send the list of every dishId in the session with the quantity that will be cooked (0 if the dish will not be cooked).

`suggestedDishId` is optional per entry. Leave it out and the backend picks the replacement dish
automatically (the category mate with the most spare quantity); send it to override that choice.

**Category budget (HTTP 400):** prepared quantity is budgeted per **category**, not per dish. For
every category that was ordered from, the sum of `preparedQuantity` across all of its dishes must be
greater than or equal to the total ordered for that category — dishes nobody ordered still count
toward the budget, and dishes left out of `preparedDishes` count as 0. If any category falls short
the entire call is rejected and nothing is saved:

```json
{
  "isSuccess": false,
  "message": "Prepared quantity for category 3fa85f64-5717-4562-b3fc-2c963f66afa6 (5) is less than the total ordered quantity (6); finalize is blocked."
}
```

Use `GET /api/sessions/{id}/dish-quantities` (below) to show the manager these totals per category
and block the button client-side before it gets that far.

### 2.1b Manager: Dish quantities grouped by category

```
GET /api/sessions/{sessionId}/dish-quantities
Auth: Manager
```

Drives the finalize screen. `categories[]` is grouped exactly the way the finalize budget is
enforced, so a shortfall is visible before the call is made. `dishes[]` is the same rows ungrouped,
kept for older callers.

```json
{
  "sessionId": "guid",
  "sessionName": "Buổi Tối vui vẻ",
  "totalOrderedQuantity": 18,
  "categories": [
    {
      "categoryId": "guid",
      "categoryName": "Đạm",
      "orderedQuantity": 10,
      "preparedQuantity": 10,
      "dishes": [
        { "dishId": "guid", "dishName": "Cá",   "categoryId": "guid", "categoryName": "Đạm", "orderedQuantity": 6, "preparedQuantity": 4 },
        { "dishId": "guid", "dishName": "Thịt", "categoryId": "guid", "categoryName": "Đạm", "orderedQuantity": 4, "preparedQuantity": 6 }
      ]
    }
  ],
  "dishes": [ /* same rows, flat */ ]
}
```

- `orderedQuantity` excludes cancelled and expired orders and refunded line items.
- `preparedQuantity` is `null` per dish (and `0` per category) until the session is finalized — before
  that, the manager is still typing those numbers in, so compute the live category total client-side
  from the inputs.
- A dish ordered before it was removed from the menu still appears, under its own category, with
  `preparedQuantity: null`.

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

**Note:** The frontend must fetch the session's dish list (excluding the current dish) so the user can pick a replacement.

### 2.3 User: Request refund from proposal

```
POST /api/changeproposals/{proposalId}/request-refund
Auth: User (must own proposal)
```

**Request body:** None (empty)

---

## 3. Changes to existing responses

### GET /api/orders/{id}

Each item in the order response now includes `itemStatus`:

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

Each dish in the session now includes `preparedQuantity`:

```json
{
  "dishes": [
    { "dishId": "guid", "quantity": 1, "preparedQuantity": null }
    // preparedQuantity: null = not finalized yet, number = quantity to be cooked
  ]
}
```

The same applies to `GET /api/sessions` (list): each dish also returns `preparedQuantity`.

---

## 4. SignalR Notification

When the Manager finalizes a session and new proposals are created, the user receives a real-time event via the SignalR hub:

**Event name:** (configured, default `ReceiveNotification`)

**Payload when a new proposal is created:**
```json
{
  "id": "guid",
  "type": "change_proposal",
  "title": "Selected dish is no longer available",
  "message": "The dish 'Chicken rice' in order #12345 is under-supplied. Please pick another dish or request a refund.",
  "referenceType": "ChangeProposal",
  "referenceId": "proposal-guid",
  "actionUrl": "/orders/order-guid/change-proposal/proposal-guid",
  "metadata": {
    "orderId": "guid",
    "proposalId": "guid",
    "currentDishId": "guid",
    "currentDishName": "Chicken rice",
    "suggestedDishId": null
  },
  "createdAtUtc": "2024-01-01T00:00:00Z"
}
```

---

## 5. Suggested UI/UX

### Manager screen (finalize orders)

In the session edit form or session detail screen, add a section:

```
┌─────────────────────────────────────────┐
│  📋 Finalize dish quantities            │
│  Deadline: 30/06/2026 10:00             │
│                                         │
│  Dish                Ordered │ To cook  │
│  ─────────────────────────────────────  │
│  🥘 Chicken rice          45 │ [50]    │
│  🥘 Pork-chop rice        30 │ [30]    │
│  🥘 Sour soup             20 │ [0]     │ ❗not cooked
│                                         │
│  [  Confirm finalization  ]             │
│                                         │
│  ⚠ 10s left to finalize                │
└─────────────────────────────────────────┘
```

- The "To cook" field defaults to the ordered quantity (editable)
- If set to 0: the dish will not be cooked and every user who ordered it receives a proposal
- If set below the ordered quantity but above 0: users who ordered this dish also receive proposals

### User screen (handle proposal)

Popup or page when the user receives the notification:

```
┌─────────────────────────────────────┐
│  ⚠ Dish is under-supplied           │
│                                     │
│  The dish "Chicken rice" in order   │
│  #12345 is short for today.         │
│                                     │
│  You can:                           │
│                                     │
│  [🔄 Swap to another dish]          │
│     → Pick from the dish list       │
│       (Pork-chop rice, Fried rice)  │
│                                     │
│  [💰 Refund this item]              │
│     → The amount is refunded to     │
│       your wallet                   │
│                                     │
│  [❌ Dismiss] (remind later)        │
└─────────────────────────────────────┘
```

### Badge on OrderItem

In the user's order list, each item can show its status:

| ItemStatus | Display |
|-----------|---------|
| 0 Pending | ✅ Processing |
| 1 Confirmed | ✅ Confirmed |
| 2 ChangePending | ⚠️ Action needed (yellow, with a button) |
| 3 Swapped | 🔄 Swapped |
| 4 Refunded | 💰 Refunded |

---

## 6. FE sequence

```
1. User opens the order detail page
   → GET /api/orders/{id}
   → Check items[].itemStatus
   → If any ChangePending → show a warning + action button

2. User receives a SignalR notification of type "change_proposal"
   → Fetch the proposal detail (or read from metadata)
   → Show a popup/dialog

3. User picks "Swap dish"
   → GET /api/sessions/{sessionId} (get the dish list)
   → User picks a replacement dish
   → POST /api/changeproposals/{proposalId}/accept { newDishId }
   → Refresh order detail → itemStatus = 3 (Swapped)

4. User picks "Refund"
   → POST /api/changeproposals/{proposalId}/request-refund
   → Refresh order detail → itemStatus = 4 (Refunded)
   → User goes to the Refund page → submits a RefundRequest (existing flow)
```

---

## 7. New session fields

When creating/updating a session, `finalizationDeadline` and `autoFinalizePolicy` can be provided:

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
| `finalizationDeadline` | DateTimeOffset (nullable) | Deadline by which the Manager must finalize orders |
| `autoFinalizePolicy` | int | `0` = AutoReject (cancel everything), `1` = AutoConfirmAll (confirm all, create proposals) |

If no deadline is set, the session has no finalization step (the previous flow applies unchanged).
