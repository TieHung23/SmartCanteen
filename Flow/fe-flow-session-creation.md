# Flow: Manager Creates a Session

> FE integration guide for the flow where a Manager creates a new meal session.

---

## 1. Overview

The Manager creates a session consisting of:
- Basic info: name, description, time window
- The list of dishes available in the session
- MealTemplates (meal plate templates) with per-category configuration

---

## 2. Sequence Diagram

```
Manager (FE)                    Backend                      DB
    │                               │                        │
    │  1. Fetch dishes               │                        │
    │  GET /api/dishes               │                        │
    │───────────────────────────────>│                        │
    │<───────────────────────────────│                        │
    │  Dish list + categories        │                        │
    │                               │                        │
    │  2. Fetch categories           │                        │
    │  GET /api/categories           │                        │
    │───────────────────────────────>│                        │
    │<───────────────────────────────│                        │
    │                               │                        │
    │  3. Manager fills the form     │                        │
  │  ┌──────────────────────────┐    │                        │
  │  │ Name: "Monday Lunch"     │    │                        │
  │  │ Opens: 10:00            │    │                        │
  │  │ Closes: 13:00           │    │                        │
  │  │ Dishes: Chicken rice,   │    │                        │
  │  │         Pork-chop rice  │    │                        │
  │  │ Template: Default        │    │                        │
  │  └──────────────────────────┘    │                        │
    │                               │                        │
    │  4. Create session             │                        │
    │  POST /api/sessions            │                        │
    │───────────────────────────────>│                        │
    │                               │                        │
    │                               │  Validate + INSERT     │
    │                               │───────────────────────>│
    │                               │<───────────────────────│
    │<───────────────────────────────│                        │
    │  201 Created + session info    │                        │
```

---

## 3. API Calls

### 3.1 Fetch dishes

```
GET /api/dishes?isActive=true
Auth: Authorize
```

**Response items:**
```json
{
  "items": [
    {
      "id": "guid",
      "name": "Chicken rice",
      "description": "Steamed rice + braised chicken",
      "priceAmount": 25.0,
      "priceCurrency": "Point",
      "isActive": true,
      "imgUrl": "https://res.cloudinary.com/...",
      "categoryId": "guid",
      "categoryName": "Main dish"
    }
  ]
}
```

### 3.2 Fetch categories

```
GET /api/categories
Auth: Authorize
```

**Response items:**
```json
{
  "items": [
    {
      "id": "guid",
      "name": "Main dish",
      "description": "Main dishes"
    }
  ]
}
```

### 3.3 Create session

```
POST /api/sessions
Auth: Authorize (Manager)
```

**Request body:**
```json
{
  "name": "Monday Lunch",
  "description": "Serving IT students",
  "availableFrom": "2026-07-01T10:00:00+07:00",
  "availableTo": "2026-07-01T13:00:00+07:00",
  "availableForOrder": "2026-06-30T22:00:00+07:00",
  "finalizationDeadline": "2026-07-01T09:30:00+07:00",
  "autoFinalizePolicy": 0,
  "mealTemplates": [
    {
      "name": "Standard plate",
      "settings": [
        { "categoryId": "guid-main-dish", "minQuantity": 1, "maxQuantity": 1, "isRequired": true },
        { "categoryId": "guid-side-dish", "minQuantity": 0, "maxQuantity": 2, "isRequired": false },
        { "categoryId": "guid-soup", "minQuantity": 0, "maxQuantity": 1, "isRequired": false }
      ]
    }
  ],
  "dishes": [
    { "dishId": "guid-chicken-rice" },
    { "dishId": "guid-porkchop-rice" },
    { "dishId": "guid-sour-soup" }
  ]
}
```

**Response 201:**
```json
{
  "value": {
    "id": "guid",
    "name": "Monday Lunch",
    "message": "Session created successfully."
  },
  "isSuccess": true,
  "message": "Session created successfully."
}
```

---

## 4. Suggested UI/UX

### Step 1: Pick dishes for the session

```
┌─── Add dishes to the session ───────────────────────────┐
│  Search dish: [________________]                         │
│                                                         │
│  Dish list (from /api/dishes)                           │
│  ┌────────────────────────────────────────────────┐    │
│  │ ☐ Chicken rice    25 Point                     │    │
│  │ ☐ Pork-chop rice  30 Point                     │    │
│  │ ☐ Sour soup       10 Point                     │    │
│  │ ☐ Water spinach   15 Point                     │    │
│  └────────────────────────────────────────────────┘    │
│                                                         │
│  [Add selected dishes to session]                       │
└─────────────────────────────────────────────────────────┘
```

### Step 2: Configure basic info

```
┌─── Create a new meal session ───────────────────────────┐
│                                                         │
│  Session name: [Monday Lunch               ]            │
│  Description:  [Serving IT students        ]            │
│                                                         │
│  Start time:   [01/07/2026 10:00]                       │
│  End time:     [01/07/2026 13:00]                       │
│  Orders open:  [30/06/2026 22:00]                       │
│                                                         │
│  ⚙️ Finalization settings (optional):                   │
│  Deadline:     [01/07/2026 09:30]                       │
│  If missed:    ▼ Cancel orders (AutoReject)             │
│                    Confirm all (AutoConfirmAll)          │
│                                                         │
  │  ──── Dishes in the session ────                        │
  │  🥘 Chicken rice                                       │
  │  🥘 Pork-chop rice                                     │
  │  🥘 Sour soup                                          │
  │                                                         │
│  ──── Meal templates ────                               │
│  [+ Add template]                                        │
│  📋 Standard plate:                                      │
│     Main dish: 1-1 (required)                           │
│     Side dish: 0-2 (optional)                           │
│     Soup: 0-1 (optional)                                │
│                                                         │
│  [Cancel]                        [Create session]       │
└─────────────────────────────────────────────────────────┘
```

### Step 3: After successful creation

- Redirect to the detail screen of the newly created session
- Or show a toast + "View session" / "Manage sessions" buttons

```
┌─────────────────────────────────────────┐
│  ✅ Session created successfully!        │
│                                         │
│  Name: Monday Lunch                     │
│  Session ID: abc-def-123                │
│  Status: Active                         │
│                                         │
│  [View details]  [Back to list]         │
└─────────────────────────────────────────┘
```

---

## 5. Validation notes

| Field | Validation |
|-------|-----------|
| `name` | Required, max 200 characters |
| `description` | Required, max 500 characters |
| `availableFrom` | Must be before `availableTo` |
| `availableTo` | Must be after `availableFrom` |
| `availableForOrder` | Must be before `availableFrom` (lets users pre-order) |
| `finalizationDeadline` | Optional; if provided, must be later than the current time |
| `autoFinalizePolicy` | `0` = AutoReject (cancel orders), `1` = AutoConfirmAll (confirm all) |
| `dishes` | Must contain at least 1 dish |
| `dishId` | Must exist and be active |
| `mealTemplates[].settings` | `maxQuantity` >= `minQuantity`, `minQuantity` >= 0 |

---

## 6. Error handling

| Case | Status | Error |
|------|--------|-------|
| Dish not found or inactive | 400 | `NullValue` + dishId |
| Template name empty | 400 | `InvalidValue` |
| Invalid min/max quantity | 400 | `InvalidValue` + description |
| Server error | 500 | `ServerError` + message |
