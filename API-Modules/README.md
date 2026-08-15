# SmartCanteen API Modules

Base URL: `/api`  
API Version: `1.0`  
Authentication: JWT Bearer Token  
All timestamps: `DateTimeOffset` (ISO 8601)  
Currency: **Point**

| Module | Doc | Base Route |
|--------|-----|------------|
| User / Auth | [`user-api.md`](user-api.md) | `/api/auth` |
| Category | [`category-api.md`](category-api.md) | `/api/categories` |
| Dish | [`dish-api.md`](dish-api.md) | `/api/dishes` |
| Session | [`session-api.md`](session-api.md) | `/api/sessions` |
| Finalize Session (Start Serving Now) — demo | [`finalize-session-now-api.md`](finalize-session-now-api.md) | `/api/sessions/{id}/finalize-now` |
| Session Dish Quantities & Menu Edit Guards | [`dish-quantities-menu-guards.md`](dish-quantities-menu-guards.md) | `/api/sessions/{id}/dish-quantities` |
| Order | [`order-api.md`](order-api.md) | `/api/orders` |
| Payment / Wallet | [`payment-api.md`](payment-api.md) | `/api/payments` |
| Wallet Transactions | — (in `payment-api.md` above) | `/api/wallet-transactions` |
| Cart | [`cart-api.md`](cart-api.md) | `/api/cart` |
| Setting | [`setting-api.md`](setting-api.md) | `/api/settings` |
| Verification | [`verification-api.md`](verification-api.md) | `/api/verification` / `/api/admin/verifications` |
| Refund | [`refund-api.md`](refund-api.md) | `/api/refunds` / `/api/manager/refunds` |
| Refund Policy | [`refundpolicy-api.md`](refundpolicy-api.md) | `/api/refund-policies` / `/api/manager/refund-policies` |
| Notification / FCM | [`notification.md`](notification.md) | `/api/notifications` / `/api/device-tokens` / `/api/admin/notifications` |
| Change Proposal | [`change-proposal-api.md`](change-proposal-api.md) | `/api/changeproposals` |
| Change Proposal Refund Flow | [`change-proposal-refund-flow.md`](change-proposal-refund-flow.md) | `/api/changeproposals` / `/api/refunds` / `/api/manager/refunds` |
| Admin Logs | — | `/api/admin/logs` |
| Robot Serving | [`robot-api.md`](robot-api.md) | `/api/robot/serving-jobs` |
| Pickup | [`pickup-api.md`](pickup-api.md) | `/api/pickup` |
| Sequence Diagrams (per controller) | [`../sequence-diagrams/`](../sequence-diagrams/README.md) | — |
| Sequence Diagram Candidates (which flows still need one) | [`sequence-diagram-candidates.md`](sequence-diagram-candidates.md) | — |

## Common Conventions

**Response Envelope:**
```json
{
  "value": {},
  "isSuccess": true,
  "message": "string",
  "error": null
}
```
On failure `value` is null, `isSuccess` false, `error` is a string code.

**Pagination** (where noted): `pageNumber=1` (min 1), `pageSize=10` (max 100)
```json
{
  "value": {
    "items": [],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 42,
    "totalPages": 5,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "isSuccess": true,
  "message": "string"
}
```
