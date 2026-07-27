# Reports / Statistics API Spec

> For FE integration. All endpoints require Manager role.

Base route:

```http
/api/manager/reports
```

Common query params:

| Param | Type | Required | Description |
|---|---:|---:|---|
| `from` | string date | No | Start date, format `yyyy-MM-dd`. Default: first day of current month in `Asia/Ho_Chi_Minh` |
| `to` | string date | No | End date, format `yyyy-MM-dd`. Default: today in `Asia/Ho_Chi_Minh` |

Common notes:

- Timezone: `Asia/Ho_Chi_Minh`.
- Date filtering is based on `CreatedAtUtc`.
- Revenue only counts orders with status `Completed`.
- Money fields are numeric VND/Point values.
- All responses are wrapped by backend `Result<T>`:

```json
{
  "value": {},
  "message": "Report summary retrieved successfully.",
  "isSuccess": true,
  "error": null
}
```

---

## 1. Summary Report

```http
GET /api/manager/reports/summary?from=2026-07-01&to=2026-07-31
```

Use this endpoint for the main dashboard and top-level reports page.

### Response

```json
{
  "value": {
    "range": {
      "from": "2026-07-01",
      "to": "2026-07-31",
      "timezone": "Asia/Ho_Chi_Minh"
    },
    "dashboard": {
      "totalOrders": 120,
      "totalRevenue": 15000000,
      "refundRate": 2.5,
      "topDish": "Com ga xoi mo",
      "newCustomers": 18,
      "totalComplaints": 6,
      "activeSessions": 2,
      "orderChange": 12.5,
      "revenueChange": 8.3,
      "refundChange": -1.2,
      "customerChange": 15.0
    },
    "revenueTrend": [
      {
        "date": "2026-07-01",
        "revenue": 1200000,
        "orders": 30
      }
    ],
    "popularDishes": [
      {
        "dishId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "dishName": "Com ga xoi mo",
        "totalOrders": 25,
        "totalQuantity": 40,
        "revenue": 2000000
      }
    ],
    "orderStats": [
      {
        "status": 0,
        "label": "Pending",
        "count": 12
      },
      {
        "status": 1,
        "label": "ReadyForPickup",
        "count": 8
      },
      {
        "status": 2,
        "label": "Completed",
        "count": 80
      },
      {
        "status": 3,
        "label": "Cancelled",
        "count": 5
      },
      {
        "status": 4,
        "label": "Preparing",
        "count": 10
      },
      {
        "status": 7,
        "label": "Expired",
        "count": 5
      }
    ],
    "refundStats": {
      "totalRefunds": 6,
      "totalRefundAmount": 300000,
      "approvedRefunds": 3,
      "rejectedRefunds": 1,
      "pendingRefunds": 2,
      "refundRate": 3.75,
      "breakdown": [
        {
          "type": "manual",
          "label": "Manual refund",
          "count": 2,
          "amount": 100000,
          "rate": 33.33
        },
        {
          "type": "item",
          "label": "Item refund",
          "count": 3,
          "amount": 150000,
          "rate": 50.0
        },
        {
          "type": "order",
          "label": "Order refund",
          "count": 1,
          "amount": 50000,
          "rate": 16.67
        }
      ]
    }
  },
  "message": "Report summary retrieved successfully.",
  "isSuccess": true
}
```

### Field Description

#### `dashboard`

| Field | Type | Description | Suggested UI |
|---|---:|---|---|
| `totalOrders` | number | Total orders in selected date range | KPI card: Total orders |
| `totalRevenue` | number | Total revenue from `Completed` orders | KPI card: Total revenue |
| `refundRate` | number | `approvedRefunds / completedOrders * 100` | KPI card: Refund rate |
| `topDish` | string | Best selling dish by quantity | KPI card / highlight |
| `newCustomers` | number | New users with role `User` in selected range | KPI card: New customers |
| `totalComplaints` | number | Total refund requests in selected range | KPI card: Complaints |
| `activeSessions` | number | Currently open active sessions | KPI card: Active sessions |
| `orderChange` | number | Order % change compared with previous same-length range | Trend badge |
| `revenueChange` | number | Revenue % change compared with previous same-length range | Trend badge |
| `refundChange` | number | Refund rate % change compared with previous same-length range | Trend badge |
| `customerChange` | number | New customer % change compared with previous same-length range | Trend badge |

If previous period value is `0`, change value returns `0`.

#### `revenueTrend[]`

| Field | Type | Description |
|---|---:|---|
| `date` | string | Date, format `yyyy-MM-dd` |
| `revenue` | number | Revenue from `Completed` orders on that date |
| `orders` | number | Number of `Completed` orders on that date |

The API returns every date between `from` and `to`, including dates with `0` revenue/orders. This helps FE render a continuous chart.

#### `popularDishes[]`

| Field | Type | Description |
|---|---:|---|
| `dishId` | uuid | Dish ID |
| `dishName` | string | Dish name |
| `totalOrders` | number | Number of distinct orders containing this dish |
| `totalQuantity` | number | Total quantity sold |
| `revenue` | number | Revenue from this dish |

Sorting rule: `totalQuantity` desc, then `revenue` desc. Max items: 10.

#### `orderStats[]`

| Status | Label | Meaning |
|---:|---|---|
| `0` | `Pending` | Order created/pending |
| `1` | `ReadyForPickup` | Order ready for pickup |
| `2` | `Completed` | Order collected/completed |
| `3` | `Cancelled` | Order cancelled |
| `4` | `Preparing` | Robot/staff preparing order |
| `7` | `Expired` | No-show / pickup expired |

#### `refundStats`

| Field | Type | Description |
|---|---:|---|
| `totalRefunds` | number | Total refund requests |
| `totalRefundAmount` | number | Total approved refund amount |
| `approvedRefunds` | number | Approved refund requests |
| `rejectedRefunds` | number | Rejected refund requests |
| `pendingRefunds` | number | Pending refund requests |
| `refundRate` | number | `approvedRefunds / completedOrders * 100` |
| `breakdown` | array | Refund breakdown by type |

Refund breakdown type:

| Type | Meaning |
|---|---|
| `manual` | Refund request not linked to change proposal |
| `item` | Refund for a specific order item |
| `order` | Refund for the whole order via change proposal |

---

## 2. Session Report

```http
GET /api/manager/reports/sessions?from=2026-07-01&to=2026-07-31
```

Use this endpoint for session/shift performance.

### Response

```json
{
  "value": {
    "range": {
      "from": "2026-07-01",
      "to": "2026-07-31",
      "timezone": "Asia/Ho_Chi_Minh"
    },
    "items": [
      {
        "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "sessionName": "Ca trua",
        "availableFrom": "2026-07-22T10:00:00+00:00",
        "availableTo": "2026-07-22T13:00:00+00:00",
        "totalOrders": 80,
        "completedOrders": 65,
        "cancelledOrders": 5,
        "expiredOrders": 2,
        "revenue": 8500000,
        "refundRequests": 3,
        "refundAmount": 150000,
        "completionRate": 81.25,
        "refundRate": 4.62
      }
    ]
  },
  "message": "Session report retrieved successfully.",
  "isSuccess": true
}
```

### Field Description

| Field | Type | Description | Suggested UI |
|---|---:|---|---|
| `sessionId` | uuid | Session ID | Table key |
| `sessionName` | string | Session name | Session column |
| `availableFrom` | datetime | Session start time | Time range |
| `availableTo` | datetime | Session end time | Time range |
| `totalOrders` | number | Total orders created in selected range for this session | Orders column |
| `completedOrders` | number | Completed orders | Completed column |
| `cancelledOrders` | number | Cancelled orders | Issue column |
| `expiredOrders` | number | Expired/no-show orders | Issue column |
| `revenue` | number | Revenue from completed orders | Revenue column |
| `refundRequests` | number | Refund requests linked to this session's orders | Refund column |
| `refundAmount` | number | Approved refund amount | Refund amount |
| `completionRate` | number | `completedOrders / totalOrders * 100` | Progress/bar |
| `refundRate` | number | `approvedRefunds / completedOrders * 100` | Rate badge |

Suggested UI:

- Table: one row per session.
- Sort default by `revenue` desc.
- Add chips/badges for high `refundRate`, high `cancelledOrders`, high `expiredOrders`.

---

## 3. Order Issues Report

```http
GET /api/manager/reports/order-issues?from=2026-07-01&to=2026-07-31
```

Use this endpoint to show operational issues.

### Response

```json
{
  "value": {
    "range": {
      "from": "2026-07-01",
      "to": "2026-07-31",
      "timezone": "Asia/Ho_Chi_Minh"
    },
    "totalOrders": 120,
    "cancelledOrders": 5,
    "expiredOrders": 2,
    "refundRequestedOrders": 6,
    "cancelledRate": 4.17,
    "expiredRate": 1.67,
    "refundRequestRate": 5.0,
    "items": [
      {
        "type": "cancelled",
        "label": "Cancelled orders",
        "count": 5,
        "rate": 4.17
      },
      {
        "type": "expired",
        "label": "Expired orders",
        "count": 2,
        "rate": 1.67
      },
      {
        "type": "refund_requested",
        "label": "Refund requested orders",
        "count": 6,
        "rate": 5.0
      }
    ]
  },
  "message": "Order issues report retrieved successfully.",
  "isSuccess": true
}
```

### Field Description

| Field | Type | Description |
|---|---:|---|
| `totalOrders` | number | Total orders in selected range |
| `cancelledOrders` | number | Orders with status `Cancelled` |
| `expiredOrders` | number | Orders with status `Expired` |
| `refundRequestedOrders` | number | Distinct orders that have refund requests |
| `cancelledRate` | number | `cancelledOrders / totalOrders * 100` |
| `expiredRate` | number | `expiredOrders / totalOrders * 100` |
| `refundRequestRate` | number | `refundRequestedOrders / totalOrders * 100` |
| `items` | array | Same issue data in chart-friendly format |

Suggested UI:

- Issue cards: Cancelled, Expired, Refund requested.
- Donut/bar chart from `items`.
- Alert if `expiredRate` or `refundRequestRate` is high.

---

## 4. Session Detail Report

```http
GET /api/manager/reports/sessions/{sessionId}
```

Use this endpoint for the detail page of one session. It does not take `from/to` because the `sessionId` already defines the timeline.

### Response

```json
{
  "value": {
    "session": {
      "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "sessionName": "Ca trua",
      "description": "Lunch session",
      "isActive": true,
      "isFinalized": false,
      "availableForOrder": "2026-07-22T07:00:00+00:00",
      "availableFrom": "2026-07-22T10:00:00+00:00",
      "availableTo": "2026-07-22T13:00:00+00:00",
      "finalizationDeadline": "2026-07-22T09:45:00+00:00",
      "finalizedAtUtc": null
    },
    "summary": {
      "totalOrders": 120,
      "pendingOrders": 7,
      "preparingOrders": 10,
      "readyForPickupOrders": 0,
      "completedOrders": 90,
      "cancelledOrders": 8,
      "expiredOrders": 5,
      "totalRevenue": 15000000,
      "refundRequests": 6,
      "approvedRefunds": 3,
      "rejectedRefunds": 1,
      "pendingRefunds": 2,
      "refundAmount": 300000,
      "completionRate": 75.0,
      "cancelRate": 6.67,
      "refundRate": 3.33,
      "averageOrderValue": 166666.67
    },
    "timeline": [
      {
        "time": "2026-07-22T07:00:00+00:00",
        "label": "Orders opened",
        "type": "orders_opened"
      }
    ],
    "orderTrend": [
      {
        "timeBucket": "2026-07-22T07:00:00+00:00",
        "orders": 10,
        "revenue": 350000
      }
    ],
    "orderStats": [
      {
        "status": 2,
        "label": "Completed",
        "count": 90
      }
    ],
    "popularDishes": [
      {
        "dishId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "dishName": "Bong cai xanh xao",
        "imgUrl": "https://...",
        "totalOrders": 50,
        "totalQuantity": 70,
        "revenue": 700000
      }
    ],
    "recentOrders": [
      {
        "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "customerName": "Nguyen Van A",
        "customerEmail": "a@example.com",
        "status": 2,
        "totalPrice": 35000,
        "itemCount": 4,
        "createdAtUtc": "2026-07-22T07:04:00+00:00"
      }
    ]
  },
  "message": "Session detail report retrieved successfully.",
  "isSuccess": true
}
```

Notes:

- `orderTrend` uses 30-minute buckets from `availableForOrder` to `availableTo` for normal sessions.
- Revenue only counts `Completed` orders.
- `recentOrders` returns the 20 newest orders in that session and includes customer name/email for manager tracking.

---

## 5. Refund Policies Report

```http
GET /api/manager/reports/refund-policies?from=2026-07-01&to=2026-07-31
```

Use this endpoint to see which refund policy/reason creates the most refund pressure.

### Response

```json
{
  "value": {
    "range": {
      "from": "2026-07-01",
      "to": "2026-07-31",
      "timezone": "Asia/Ho_Chi_Minh"
    },
    "items": [
      {
        "policyCode": "ITEM_REFUND_POLICY_CODE",
        "policyName": "Hoan tien theo mon",
        "totalRequests": 8,
        "approvedRequests": 5,
        "rejectedRequests": 2,
        "pendingRequests": 1,
        "totalAmount": 450000,
        "approvedAmount": 300000,
        "approvalRate": 62.5
      }
    ]
  },
  "message": "Refund policies report retrieved successfully.",
  "isSuccess": true
}
```

### Field Description

| Field | Type | Description |
|---|---:|---|
| `policyCode` | string | Refund policy code snapshot |
| `policyName` | string | Refund policy name snapshot |
| `totalRequests` | number | Total refund requests for this policy |
| `approvedRequests` | number | Approved requests |
| `rejectedRequests` | number | Rejected requests |
| `pendingRequests` | number | Pending requests |
| `totalAmount` | number | Total requested refund amount |
| `approvedAmount` | number | Approved refund amount |
| `approvalRate` | number | `approvedRequests / totalRequests * 100` |

Suggested UI:

- Table grouped by policy.
- Sort by `totalRequests` desc or `approvedAmount` desc.
- Use stacked bar for approved/rejected/pending.

---

## FE Page Suggestion

### Manager Dashboard `/manager`

Call:

```http
GET /api/manager/reports/summary?from=today&to=today
```

Important: FE should convert `today` to a real date string before calling API, for example:

```http
GET /api/manager/reports/summary?from=2026-07-22&to=2026-07-22
```

Show:

- KPI cards from `dashboard`.
- Revenue chart from `revenueTrend`.
- Top dishes from `popularDishes`.
- Order status chart from `orderStats`.
- Refund summary from `refundStats`.

### Manager Reports `/manager/reports`

Common date filters:

| Filter | API params |
|---|---|
| Today | `from=yyyy-MM-dd&to=yyyy-MM-dd` |
| This week | `from=<start-of-week>&to=<end-of-week>` |
| This month | `from=<first-day-of-month>&to=<last-day-of-month>` |
| Custom | `from=<date-picker-from>&to=<date-picker-to>` |

Recommended calls:

```http
GET /api/manager/reports/summary?from=&to=
GET /api/manager/reports/sessions?from=&to=
GET /api/manager/reports/sessions/{sessionId}
GET /api/manager/reports/order-issues?from=&to=
GET /api/manager/reports/refund-policies?from=&to=
```

Suggested tabs:

| Tab | Endpoint |
|---|---|
| Overview | `summary` |
| Sessions | `sessions` |
| Session Detail | `sessions/{sessionId}` |
| Issues | `order-issues` |
| Refund Policies | `refund-policies` |

---

## Empty Data Behavior

If there is no data in the selected range:

- Number fields return `0`.
- String fields return `""`.
- List fields return `[]`, except `revenueTrend` returns every date in range with `0` values.
- Rate/change fields return `0`.

---

## Important FE Notes

- Do not send `from=today` or `to=today` directly. Convert to `yyyy-MM-dd`.
- Display `totalRevenue`, not profit. This API does not calculate profit/cost.
- Revenue only includes `Completed` orders.
- `Completed` means the order has been collected/completed, or manager manually changed order status to `Completed`.
- `activeSessions` is current-time based, not selected-range based.
- `revenueTrend` length depends on `from/to`; it is not hardcoded.
