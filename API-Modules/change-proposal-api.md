# SmartCanteen Change Proposal API

Base URL: `/api/changeproposals`  
Auth: `[Authorize]`  
API Version: `1.0`

Proposals are created automatically by `POST /api/sessions/{id}/finalize` when a dish is under-supplied. Users accept or request refund for their proposals.

---

## `POST /api/changeproposals/{id}/accept`
User accepts the swap — missing items are removed and substitute items (if any) are confirmed.

```json
{
  "value": { "id": "guid", "message": "string" },
  "isSuccess": true
}
```

## `POST /api/changeproposals/{id}/request-refund`
User rejects the swap and requests a refund for the missing items.

```json
{
  "value": { "id": "guid", "message": "string" },
  "isSuccess": true
}
```
