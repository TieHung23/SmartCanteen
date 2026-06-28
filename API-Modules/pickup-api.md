# SmartCanteen Pickup API

Base URL: `/api/pickup`  
Auth: `[Authorize]`  
API Version: `1.0`

Pickup shelf (kệ pickup): staff assigns trays to slots; students scan QR to collect.

---

## `POST /api/pickup/assign`
Staff/sensor assigns a tray (order) to a pickup slot.

```json
{ "slotCode": "string", "orderId": "guid", "trayCode": "string" }
```

```json
{
  "value": { "slotId": "guid", "slotCode": "string", "orderId": "guid", "trayId": "guid" },
  "isSuccess": true
}
```

## `POST /api/pickup/collect`
Student scans QR (encodes `orderId`) to collect order. Releases slot + tray.

```json
{ "orderId": "guid" }
```

```json
{
  "value": { "orderId": "guid", "slotCode": "string | null" },
  "isSuccess": true
}
```
