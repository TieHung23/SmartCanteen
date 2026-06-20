# SmartCanteen Notification API

Base URL: `/api`  
Auth: All endpoints require `Authorization: Bearer <accessToken>` unless noted.  
API Version: `1.0`

---

## Overview

Three delivery paths:
- **Stored (DB)** — FE reads history via HTTP API.
- **Realtime (SignalR)** — Web/mobile foreground.
- **Push (FCM)** — Mobile background/closed.

Flow:
```
Business event / Manager creates notification
  → Save to DB
  → SignalR event if client connected
  → FCM push if user has active device tokens
```

SignalR and FCM are best-effort. Use `GET /api/notifications` as source of truth.

## Notification Model

```json
{
  "id": "guid",
  "type": "Order.StatusChanged",
  "title": "Order status updated",
  "message": "Your order status changed to Completed.",
  "referenceType": "Order",
  "referenceId": "guid",
  "actionUrl": "/orders/guid",
  "dataJson": "{\"status\":\"Completed\"}",
  "isRead": false,
  "readAtUtc": null,
  "createdAtUtc": "..."
}
```

---

## User Endpoints

### `GET /api/notifications`
**Query:** `?pageNumber=1&pageSize=10&isRead=true&type=Order.StatusChanged`

```json
{
  "value": {
    "items": [ /* Notification model objects */ ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  },
  "isSuccess": true,
  "message": "Notifications retrieved successfully."
}
```

### `GET /api/notifications/unread-count`
```json
{
  "value": { "count": 3 },
  "isSuccess": true,
  "message": "Unread notification count retrieved successfully."
}
```

### `PATCH /api/notifications/{id}/read`
Marks one notification as read. `404` if not found or not owned by current user.

### `PATCH /api/notifications/read-all`
Marks all unread as read.
```json
{
  "value": { "updatedCount": 2 },
  "isSuccess": true,
  "message": "Notifications marked as read."
}
```

### `DELETE /api/notifications/{id}`
Soft delete. `404` if not found.

---

## Device Token APIs (Mobile FCM)

### `POST /api/device-tokens`
Register or refresh FCM token.

```json
{
  "token": "fcm-token-from-mobile",
  "platform": "Android",
  "deviceId": "optional-stable-device-id",
  "appVersion": "1.0.0"
}
```
`platform` must be `Android` or `iOS`.

```json
{
  "value": {
    "id": "guid",
    "platform": "Android",
    "deviceId": "...",
    "appVersion": "1.0.0",
    "isActive": true,
    "lastUsedAtUtc": "..."
  },
  "isSuccess": true,
  "message": "Device token registered successfully."
}
```

### `DELETE /api/device-tokens`
Revoke a device token (call on logout). At least one of `token` or `deviceId` required.

```json
{ "token": "fcm-token-from-mobile" }
```
or
```json
{ "deviceId": "optional-stable-device-id" }
```

```json
{
  "value": { "revokedCount": 1 },
  "isSuccess": true,
  "message": "Device token unregistered successfully."
}
```

---

## Manager Endpoints

### `POST /api/admin/notifications`
Auth: `[Authorize(Roles = "Manager")]`

```json
{
  "recipientId": "user-guid",
  "type": "System.Announcement",
  "title": "System maintenance",
  "message": "The canteen system will be under maintenance tonight.",
  "referenceType": null,
  "referenceId": null,
  "actionUrl": "/notifications",
  "data": { "source": "admin" }
}
```

---

## SignalR Realtime

Hub path: `/hubs/notifications`  
Client event: `NotificationReceived`

```js
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5265/hubs/notifications", {
    accessTokenFactory: () => accessToken
  })
  .withAutomaticReconnect()
  .build();

connection.on("NotificationReceived", notification => {
  // Prepend to list, show toast, update unread count
});

await connection.start();
```

Recommended web flow:
1. Load `GET /api/notifications` + unread count
2. Start SignalR
3. On `NotificationReceived`, prepend + increment badge
4. On tap: `PATCH /api/notifications/{id}/read`, navigate via `actionUrl`

---

## Mobile FCM Flow

1. Login → get FCM token → `POST /api/device-tokens`
2. On FCM token refresh → `POST /api/device-tokens` again
3. On logout → `DELETE /api/device-tokens`
4. On native push tap → navigate via payload fields

FCM data payload:
```json
{
  "notificationId": "guid",
  "type": "Order.StatusChanged",
  "isRead": "False",
  "createdAtUtc": "...",
  "referenceType": "Order",
  "referenceId": "guid",
  "actionUrl": "/orders/guid",
  "dataJson": "{\"status\":\"Completed\"}"
}
```

FCM notification title/body:
```json
{
  "title": "Order status updated",
  "body": "Your order status changed to Completed."
}
```

---

## Automatic Business Notifications

Backend creates notifications for:
- `Order.Created`, `Order.StatusChanged`
- `Payment.Completed`
- `Refund.Submitted`, `Refund.Approved`, `Refund.Rejected`
- `Verification.Submitted`, `Verification.Approved`, `Verification.Rejected`
