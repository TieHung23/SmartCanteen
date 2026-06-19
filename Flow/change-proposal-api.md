# SmartCanteen Change Proposal API

> Generated from `API-Document.md`. Run `.\tools\generate-api-modules.ps1` after updating the main document.

# SmartCanteen API Documentation

Base URL: `/api`  
API Version: `1.0`  
All timestamps: `DateTimeOffset` (ISO 8601)  
Currency: **Point** (no currency field exposed)

Every response is wrapped in a standard envelope:

```json
{
  "value": { },
  "isSuccess": true,
  "isFailure": false,
  "message": "string",
  "error": null
}
```

On failure `value` is null, `isSuccess` false, `error` is a string code.

---

## Change Proposals

When a Manager finalizes a session, any order item whose dish has insufficient prepared quantity is automatically marked `ChangePending` and a `ChangeProposal` is created. The user must either accept a suggested replacement dish or request a refund for that item.

**Proposal lifecycle:**

```
Manager finalizes session
     ↓
OrderItem.MarkChangePending() + Proposal created (WaitingResponse)
     ↓
User chooses:
   ├─ POST /api/changeproposals/{id}/accept   → Item swapped, Proposal Accepted
   └─ POST /api/changeproposals/{id}/request-refund → Item Refunded, Proposal RefundRequested
```

### `POST /api/changeproposals/{id}/accept`
**Auth:** Authorize (User — must own the proposal)

**Request body:**
```json
{
  "newDishId": "guid"
}
```

**Response:**
```json
{
  "value": {
    "message": "Dish swapped successfully."
  },
  "isSuccess": true
}
```

### `POST /api/changeproposals/{id}/request-refund`
**Auth:** Authorize (User — must own the proposal)

**Request body:** None

**Response:**
```json
{
  "value": {
    "message": "Refund requested. Please submit a refund request through the refund workflow."
  },
  "isSuccess": true
}
```
---

## Flow Diagram (Sequence)

```
User                    Backend                      Manager
  │                        │                            │
  │  Place order           │                            │
  │───────────────────────>│                            │
  │                        │                            │
  │                        │    Finalize session         │
  │                        │  (with prepared quantities) │
  │                        │<───────────────────────────│
  │                        │                            │
  │                        │  Validate prepared qty vs   │
  │                        │  ordered qty for each item  │
  │                        │  - Enough → Item.Confirmed  │
  │                        │  - Not enough → Proposal    │
  │                        │                            │
  │  Receive notification  │                            │
  │  (new proposal)        │                            │
  │<───────────────────────│                            │
  │                        │                            │
  │  Accept / RequestRefund│                            │
  │───────────────────────>│                            │
  │                        │                            │
  │  Receive result        │                            │
  │<───────────────────────│                            │
```

---

## Related Statuses

| Entity       | Field        | Values                                                                 |
|-------------|--------------|------------------------------------------------------------------------|
| OrderItem   | ItemStatus   | `0` Pending → `1` Confirmed → `2` ChangePending → `3` Swapped → `4` Refunded |
| ChangeProposal | ProposalStatus | `0` WaitingResponse → `1` Accepted → `2` RefundRequested                |
| Session     | IsFinalized  | `false` → `true` (set when manager finalizes)                          |
| SessionDish | PreparedQuantity | `null` (not yet) → `int` (set during finalization)                  |

---

## Frontend Integration Notes

1. **After order creation** — items are in `Pending` status. No action needed from user yet.
2. **User receives a real-time notification** (via SignalR) when a proposal is created. The notification payload includes:
   ```json
   {
     "type": "change_proposal",
     "proposalId": "guid",
     "orderId": "guid",
     "currentDishId": "guid",
     "suggestedDishId": "guid | null"
   }
   ```
3. **Display proposal to user** — show the dish that needs changing and let them pick another dish from the menu (same session) or choose refund.
4. **`GET /api/orders/{id}`** response now includes `itemStatus` per order item:
   ```json
   {
     "items": [
       { "dishId": "guid", "quantity": 1, "unitPrice": 0.0, "itemStatus": 0 }
     ]
   }
   ```
5. **On session detail** — each dish now returns optional `preparedQuantity`:
   ```json
   {
     "dishes": [
       { "dishId": "guid", "quantity": 1, "preparedQuantity": null }
     ]
   }
   ```
