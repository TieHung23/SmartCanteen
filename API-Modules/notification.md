# SmartCanteen Notification API

Base URL: `/api`  
Auth: all endpoints require `Authorization: Bearer <accessToken>` unless noted.  
Response envelope:

```json
{
  "value": { },
  "isSuccess": true,
  "isFailure": false,
  "message": "string",
  "error": null
}
```

## Overview

Notification currently supports three delivery paths:

- Stored notification in database: FE gets history through HTTP API.
- Realtime notification: web/mobile foreground can listen through SignalR.
- Native mobile push: mobile background/closed receives FCM push after registering an FCM token.

Delivery flow:

```text
Business event / Manager creates notification
  -> Save notification to DB
  -> Send SignalR event if client is connected
  -> Send FCM push if user has active device tokens
```

SignalR and FCM are best-effort. The notification is always saved before realtime/push delivery, so FE should use `GET /api/notifications` as the source of truth.

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
  "createdAtUtc": "2026-06-16T14:34:01.0000000+00:00"
}
```

Field notes:

- `type`: notification category/event type.
- `referenceType`: related business object type, for example `Order`, `Payment`, `RefundRequest`, `VerificationRequest`.
- `referenceId`: ID of the related object. It is nullable.
- `actionUrl`: route FE should open when user taps/clicks the notification. It is nullable.
- `dataJson`: optional extra data serialized as JSON string.
- `isRead`: unread/read state scoped to current user.

## User Notification APIs

### `GET /api/notifications`

Gets notifications for the authenticated user.

Query params:

| Name | Type | Required | Notes |
|---|---:|---:|---|
| `pageNumber` | number | No | Default from shared pagination params |
| `pageSize` | number | No | Default from shared pagination params |
| `isRead` | boolean | No | `true` for read, `false` for unread |
| `type` | string | No | Exact type filter, for example `Order.StatusChanged` |

Example:

```http
GET /api/notifications?pageNumber=1&pageSize=10&isRead=false
Authorization: Bearer <userToken>
```

Success response:

```json
{
  "value": {
    "items": [
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
        "createdAtUtc": "2026-06-16T14:34:01.0000000+00:00"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  },
  "isSuccess": true,
  "isFailure": false,
  "message": "Notifications retrieved successfully.",
  "error": null
}
```

### `GET /api/notifications/unread-count`

Gets unread notification count for the authenticated user.

```http
GET /api/notifications/unread-count
Authorization: Bearer <userToken>
```

Success response:

```json
{
  "value": {
    "count": 3
  },
  "isSuccess": true,
  "isFailure": false,
  "message": "Unread notification count retrieved successfully.",
  "error": null
}
```

### `PATCH /api/notifications/{id}/read`

Marks one notification as read. User can only update their own notification.

```http
PATCH /api/notifications/00000000-0000-0000-0000-000000000000/read
Authorization: Bearer <userToken>
```

Success response value is the updated notification.

Common errors:

- `404 NotificationNotFound`: notification does not exist or does not belong to the current user.

### `PATCH /api/notifications/read-all`

Marks all unread notifications as read for the authenticated user.

```http
PATCH /api/notifications/read-all
Authorization: Bearer <userToken>
```

Success response:

```json
{
  "value": {
    "updatedCount": 2
  },
  "isSuccess": true,
  "isFailure": false,
  "message": "Notifications marked as read.",
  "error": null
}
```

### `DELETE /api/notifications/{id}`

Deletes one notification for the authenticated user. This is a soft delete.

```http
DELETE /api/notifications/00000000-0000-0000-0000-000000000000
Authorization: Bearer <userToken>
```

Success response:

```json
{
  "value": {
    "id": "00000000-0000-0000-0000-000000000000"
  },
  "isSuccess": true,
  "isFailure": false,
  "message": "Notification deleted successfully.",
  "error": null
}
```

## Device Token APIs for Mobile FCM

Mobile must call this after login and whenever Firebase refreshes the FCM token.

### `POST /api/device-tokens`

Registers or refreshes the current device FCM token for the authenticated user.

Request body:

```json
{
  "token": "fcm-token-from-mobile",
  "platform": "Android",
  "deviceId": "optional-stable-device-id",
  "appVersion": "1.0.0"
}
```

Validation:

- `token`: required, max 4096 chars.
- `platform`: required, must be `Android` or `iOS`.
- `deviceId`: optional, max 256 chars.
- `appVersion`: optional, max 64 chars.

Success response:

```json
{
  "value": {
    "id": "guid",
    "platform": "Android",
    "deviceId": "optional-stable-device-id",
    "appVersion": "1.0.0",
    "isActive": true,
    "lastUsedAtUtc": "2026-06-16T14:34:01.0000000+00:00"
  },
  "isSuccess": true,
  "isFailure": false,
  "message": "Device token registered successfully.",
  "error": null
}
```

Mobile usage:

- Call after successful login.
- Call again when Firebase SDK reports token refresh.
- Use the same `deviceId` for the same physical device if the app has one.

### `DELETE /api/device-tokens`

Revokes a device token for the authenticated user. Call this on logout.

Request body with token:

```json
{
  "token": "fcm-token-from-mobile"
}
```

Or request body with device ID:

```json
{
  "deviceId": "optional-stable-device-id"
}
```

Validation:

- At least one of `token` or `deviceId` is required.

Success response:

```json
{
  "value": {
    "revokedCount": 1
  },
  "isSuccess": true,
  "isFailure": false,
  "message": "Device token unregistered successfully.",
  "error": null
}
```

## Manager Manual Notification API

This endpoint is for Manager manual announcements and testing. Normal business notifications are created automatically by backend workflows.

### `POST /api/admin/notifications`

Auth: `Manager`

Request body:

```json
{
  "recipientId": "user-guid",
  "type": "System.Announcement",
  "title": "System maintenance",
  "message": "The canteen system will be under maintenance tonight.",
  "referenceType": null,
  "referenceId": null,
  "actionUrl": "/notifications",
  "data": {
    "source": "admin"
  }
}
```

Validation notes:

- `recipientId` must be an existing user ID.
- `type`, `title`, `message` are required.
- `referenceId` must be a valid GUID if provided.
- If `referenceId` is provided, `referenceType` should also be provided.
- `actionUrl` should be an app route such as `/orders/{id}`.

Success response value is the created notification.

## SignalR Realtime

Hub path:

```text
/hubs/notifications
```

Client event name:

```text
NotificationReceived
```

JavaScript example:

```js
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5265/hubs/notifications", {
    accessTokenFactory: () => accessToken
  })
  .withAutomaticReconnect()
  .build();

connection.on("NotificationReceived", notification => {
  console.log("Notification received:", notification);
  // FE should add notification to list, show toast, and update unread count.
});

await connection.start();
```

Recommended web behavior:

1. After login/load app, call `GET /api/notifications`.
2. Call `GET /api/notifications/unread-count`.
3. Start SignalR connection.
4. On `NotificationReceived`, prepend item to notification list and increment unread badge.
5. When user opens/clicks a notification, call `PATCH /api/notifications/{id}/read`.
6. Navigate using `actionUrl` when present.

## Mobile FCM Flow

Recommended mobile behavior:

1. User logs in and receives API access token.
2. Mobile Firebase SDK gets FCM token.
3. Mobile calls `POST /api/device-tokens`.
4. Mobile listens for FCM token refresh and calls `POST /api/device-tokens` again.
5. On logout, mobile calls `DELETE /api/device-tokens`.
6. When user taps native push, mobile navigates based on payload fields.

FCM push data payload contains:

```json
{
  "notificationId": "guid",
  "type": "Order.StatusChanged",
  "isRead": "False",
  "createdAtUtc": "2026-06-16T14:34:01.0000000+00:00",
  "referenceType": "Order",
  "referenceId": "guid",
  "actionUrl": "/orders/guid",
  "dataJson": "{\"status\":\"Completed\"}"
}
```

Backend also sends FCM notification title/body:

```json
{
  "title": "Order status updated",
  "body": "Your order status changed to Completed."
}
```

## Automatic Business Notifications

Backend already creates notifications for:

- `Order.Created`
- `Order.StatusChanged`
- `Payment.Completed`
- `Refund.Submitted`
- `Refund.Approved`
- `Refund.Rejected`
- `Verification.Submitted`
- `Verification.Approved`
- `Verification.Rejected`

FE does not call admin notification API for these. FE only performs the normal business action, then listens through SignalR/FCM and refreshes notification API state.

## FE Test Checklist

Web:

1. Login.
2. Call `GET /api/notifications`.
3. Call `GET /api/notifications/unread-count`.
4. Connect SignalR.
5. Create a notification from Swagger/Manager account.
6. Confirm `NotificationReceived` fires.
7. Mark read and confirm unread count decreases.

Mobile:

1. Login.
2. Get FCM token from Firebase SDK.
3. Call `POST /api/device-tokens`.
4. Put app in background or close it.
5. Create a notification from Swagger/Manager account or trigger a business event.
6. Confirm native push appears.
7. Tap notification and navigate using `actionUrl`.
8. Logout and call `DELETE /api/device-tokens`.
