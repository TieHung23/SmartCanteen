# Need More Data — API Response Fields Missing for UI Display

Tracks which API endpoints return **insufficient data** to render human-readable UI.

## Priority Legend
- **P0** — Full raw ID shown to user (no fallback, no truncation)
- **P1** — Fallback truncates ID but no proper name available
- **P2** — Truncated ID shown, minor UX impact

---

## ✅ Status Key
- **Fixed** — field added in previous work (session dish enrichment)
- **Needs implementation** — backend code change required
- **Needs doc update** — API doc now specifies the field but backend hasn't added it yet

---

## 1. GET /api/sessions/{id} — Session Detail

| Field | Status | Notes |
|---|---|---|
| `dishes[].dishName`, `imgUrl`, `priceAmount`, `priceCurrency`, `categoryId` | **Fixed** | Already added to `SessionDishDto` + batch-loaded in handler |
| `mealTemplates[].settings[].categoryName` | **Needs implementation** | Need to join `Category` table in handler and add field to `MealSettingDto` |

---

## 2. GET /api/orders/{id} — Order Detail

| Field | Status | Notes |
|---|---|---|
| `items[].dishName`, `imgUrl` | **Needs implementation** | Handler joins `OrderItem.DishId` → `Dish` table |
| `order.id` | Truncated client-side | Acceptable as-is |

---

## 3. GET /api/refunds/{id} — Refund Detail (Manager)

| Field | Status | Notes |
|---|---|---|
| `userName`, `userEmail`, `studentId` | **Needs implementation** | Handler joins `RefundRequest.UserId` → `User` table |
| Human-readable order reference | **Needs implementation** | Not critical if order detail is available separately |

---

## 4. GET /api/refunds — Refund List (Manager)

| Field | Status | Notes |
|---|---|---|
| `items[].userName`, `studentId` | **Needs implementation** | Handler joins `RefundRequest.UserId` → `User` table |

---

## 5. POST /api/orders — Create Order (Checkout Response)

| Field | Status | Notes |
|---|---|---|
| Human-readable transaction reference | **Needs doc update** | `message` field already exists and can be used |

---

## 6. GET /api/cart — Cart

| Field | Status | Notes |
|---|---|---|
| `data.sessions[].items[].dishName`, `imgUrl` | **Needs implementation** | Cart stores only `DishId` + `Quantity`; enrichment requires loading dishes when serializing cart response |
| `data.sessions[].categoryName` | **Needs implementation** | Need to resolve category from dish |

---

## 7. GET /api/orders (List) — Order List Item

| Field | Status | Notes |
|---|---|---|
| Order number/label | Truncated client-side | Acceptable as-is |

---

## 8. Template Settings / Cart — Category Name

| Source | Status | Notes |
|---|---|---|
| `mealTemplates[].settings[].categoryId` without `categoryName` | **Needs implementation** | Simple `.Include(x => x.Category)` + `Select` |
| Cart items `categoryId` without `categoryName` | **Needs implementation** | Requires resolving dish → category chain |
