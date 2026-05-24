# SmartCanteen API Reference

Generated from controller analysis - May 24, 2026

---

## GET /api/categories

- Controller: SC.Api.Controllers.CategoriesController
- Action: GetAllCategories
- RequestType: GetAllCategoriesQuery (pagination + optional name filter)
- ResponseType: Result<PaginatedList<GetAllCategoriesResponse>>

### Request Query Parameters

```
pageNumber: int (default: 1)
pageSize: int (default: 10)
name: string? (optional filter)
```

### Response JSON

```json
{
  "success": true,
  "data": {
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 5,
    "items": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "name": "Meat",
        "description": "Meat category"
      }
    ]
  },
  "message": "Categories retrieved successfully."
}
```

### Notes

- Status codes: 200 (success), 400 (failure)
- Authorization: None required
- Source: SC.Api/Controllers/CategoriesController.cs

---

## GET /api/categories/{id}

- Controller: SC.Api.Controllers.CategoriesController
- Action: GetCategoryById
- RequestType: Guid (route parameter)
- ResponseType: unknown (not implemented - throws Exception)

### Notes

- Status codes: unknown
- Source: SC.Api/Controllers/CategoriesController.cs
- **Status**: NOT IMPLEMENTED - method throws Exception

---

## GET /api/meals

- Controller: SC.Api.Controllers.MealsController
- Action: GetAllMeals
- RequestType: GetAllMealsQuery (pagination + filters)
- ResponseType: Result<PaginatedList<GetAllMealsResponse>>

### Request Query Parameters

```
pageNumber: int (default: 1)
pageSize: int (default: 10)
name: string? (optional filter)
isActive: bool? (optional filter)
```

### Response JSON

```json
{
  "success": true,
  "data": {
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 3,
    "items": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440001",
        "name": "Lunch Set A",
        "description": "Delicious lunch combo",
        "priceAmount": 45000,
        "priceCurrency": "VND",
        "isActive": true,
        "availableFrom": "2026-05-24T10:00:00Z",
        "availableTo": "2026-05-24T14:00:00Z",
        "availableForOrder": "2026-05-24T09:30:00Z"
      }
    ]
  },
  "message": "Meals retrieved successfully."
}
```

### Notes

- Status codes: 200 (success), 400 (failure)
- Authorization: None required ([AllowAnonymous])
- Source: SC.Api/Controllers/MealsController.cs

---

## GET /api/meals/{id}

- Controller: SC.Api.Controllers.MealsController
- Action: GetMealById
- RequestType: Guid (route parameter)
- ResponseType: Result<GetMealByIdResponse>

### Response JSON

```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "name": "Lunch Set A",
    "description": "Delicious lunch combo",
    "priceAmount": 45000,
    "priceCurrency": "VND",
    "isActive": true,
    "availableFrom": "2026-05-24T10:00:00Z",
    "availableTo": "2026-05-24T14:00:00Z",
    "availableForOrder": "2026-05-24T09:30:00Z",
    "mealSettings": [
      {
        "categoryId": "550e8400-e29b-41d4-a716-446655440010",
        "quantity": 2
      }
    ]
  },
  "message": "Meal retrieved successfully."
}
```

### Notes

- Status codes: 200 (success), 404 (not found)
- Authorization: None required ([AllowAnonymous])
- Source: SC.Api/Controllers/MealsController.cs

---

## POST /api/meals

- Controller: SC.Api.Controllers.MealsController
- Action: CreateMeal
- RequestType: CreateMealCommand
- ResponseType: Result<CreateMealResponse>

### Request JSON

```json
{
  "name": "Lunch Set A",
  "description": "Delicious lunch combo",
  "priceAmount": 45000,
  "priceCurrency": "VND",
  "availableFrom": "2026-05-24T10:00:00Z",
  "availableTo": "2026-05-24T14:00:00Z",
  "availableForOrder": "2026-05-24T09:30:00Z",
  "mealSettings": [
    {
      "categoryId": "550e8400-e29b-41d4-a716-446655440010",
      "quantity": 2
    },
    {
      "categoryId": "550e8400-e29b-41d4-a716-446655440011",
      "quantity": 1
    }
  ]
}
```

### Response JSON

```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "name": "Lunch Set A",
    "message": "Meal created successfully."
  },
  "message": "Meal created successfully."
}
```

### Notes

- Status codes: 201 (created), 400 (validation failure)
- Authorization: Required ([Authorize])
- Source: SC.Api/Controllers/MealsController.cs

---

## PUT /api/meals/{id}

- Controller: SC.Api.Controllers.MealsController
- Action: UpdateMeal
- RequestType: UpdateMealCommand (id in route + body)
- ResponseType: Result<UpdateMealResponse>

### Request JSON

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "name": "Updated Lunch Set",
  "description": "Updated description",
  "priceAmount": 50000,
  "priceCurrency": "VND",
  "isActive": true,
  "availableFrom": "2026-05-24T10:00:00Z",
  "availableTo": "2026-05-24T14:00:00Z",
  "availableForOrder": "2026-05-24T09:30:00Z",
  "mealSettings": [
    {
      "categoryId": "550e8400-e29b-41d4-a716-446655440010",
      "quantity": 3
    }
  ]
}
```

### Response JSON

```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "name": "Updated Lunch Set",
    "message": "Meal updated successfully."
  },
  "message": "Meal updated successfully."
}
```

### Notes

- Status codes: 200 (success), 400 (validation/ID mismatch failure)
- Authorization: Required ([Authorize])
- Source: SC.Api/Controllers/MealsController.cs

---

## DELETE /api/meals/{id}

- Controller: SC.Api.Controllers.MealsController
- Action: DeleteMeal
- RequestType: Guid (route parameter)
- ResponseType: Result<DeleteMealResponse>

### Response JSON

```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "message": "Meal deleted successfully."
  },
  "message": "Meal deleted successfully."
}
```

### Notes

- Status codes: 200 (success), 400 (failure)
- Authorization: Required ([Authorize])
- Source: SC.Api/Controllers/MealsController.cs

---

## GET /api/orders

- Controller: SC.Api.Controllers.OrdersController
- Action: GetAllOrders
- RequestType: GetAllOrdersQuery (pagination + filters)
- ResponseType: Result<PaginatedList<GetAllOrdersResponse>>

### Request Query Parameters

```
pageNumber: int (default: 1)
pageSize: int (default: 10)
userId: Guid? (optional - filters to current user if not specified)
status: int? (optional - 0=Pending, 1=ReadyForPickup, 2=Completed, 3=Cancelled)
```

### Response JSON

```json
{
  "success": true,
  "data": {
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 2,
    "items": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440100",
        "mealId": "550e8400-e29b-41d4-a716-446655440001",
        "userId": "550e8400-e29b-41d4-a716-446655440200",
        "status": 0,
        "totalPrice": 80000,
        "currency": "VND",
        "itemCount": 3,
        "createdAtUtc": "2026-05-24T10:00:00Z"
      }
    ]
  },
  "message": "Orders retrieved successfully."
}
```

### Notes

- Status codes: 200 (success), 400 (failure)
- Authorization: Required ([Authorize])
- Source: SC.Api/Controllers/OrdersController.cs

---

## GET /api/orders/{id}

- Controller: SC.Api.Controllers.OrdersController
- Action: GetOrderById
- RequestType: Guid (route parameter)
- ResponseType: Result<GetOrderByIdResponse>

### Response JSON

```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440100",
    "mealId": "550e8400-e29b-41d4-a716-446655440001",
    "userId": "550e8400-e29b-41d4-a716-446655440200",
    "status": 0,
    "totalPrice": 80000,
    "currency": "VND",
    "items": [
      {
        "dishId": "550e8400-e29b-41d4-a716-446655440050",
        "quantity": 2,
        "unitPrice": 25000,
        "currency": "VND"
      }
    ],
    "createdAtUtc": "2026-05-24T10:00:00Z",
    "updatedAtUtc": null
  },
  "message": "Order retrieved successfully."
}
```

### Notes

- Status codes: 200 (success), 404 (not found)
- Authorization: Required ([Authorize])
- Source: SC.Api/Controllers/OrdersController.cs

---

## POST /api/orders

- Controller: SC.Api.Controllers.OrdersController
- Action: CreateOrder
- RequestType: CreateOrderCommand
- ResponseType: Result<CreateOrderResponse>

### Request JSON

```json
{
  "mealId": "550e8400-e29b-41d4-a716-446655440001",
  "items": [
    {
      "dishId": "550e8400-e29b-41d4-a716-446655440050",
      "quantity": 2,
      "unitPrice": 25000
    },
    {
      "dishId": "550e8400-e29b-41d4-a716-446655440051",
      "quantity": 1,
      "unitPrice": 30000
    }
  ]
}
```

### Response JSON

```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440100",
    "totalPrice": 80000,
    "currency": "VND",
    "message": "Order created successfully and wallet debited.",
    "userRemainingBalance": 120000
  },
  "message": "Order created successfully."
}
```

### Notes

- Status codes: 201 (created), 400 (validation/insufficient balance)
- Authorization: Required ([Authorize])
- **Important**: Automatically deducts totalPrice from user's wallet balance
- Validation: User must have sufficient balance or request fails
- Source: SC.Api/Controllers/OrdersController.cs

---

## PUT /api/orders/{id}

- Controller: SC.Api.Controllers.OrdersController
- Action: UpdateOrder
- RequestType: UpdateOrderCommand (id in route + body)
- ResponseType: Result<UpdateOrderResponse>

### Request JSON

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440100",
  "status": 1
}
```

### Status Values

- 0: Pending
- 1: ReadyForPickup
- 2: Completed
- 3: Cancelled

### Response JSON

```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440100",
    "status": 1,
    "message": "Order status updated to ReadyForPickup."
  },
  "message": "Order updated successfully."
}
```

### Notes

- Status codes: 200 (success), 400 (invalid status)
- Authorization: Required ([Authorize])
- Source: SC.Api/Controllers/OrdersController.cs

---

## DELETE /api/orders/{id}

- Controller: SC.Api.Controllers.OrdersController
- Action: DeleteOrder
- RequestType: Guid (route parameter)
- ResponseType: Result<DeleteOrderResponse>

### Response JSON

```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440100",
    "message": "Order deleted successfully."
  },
  "message": "Order deleted successfully."
}
```

### Notes

- Status codes: 200 (success), 400 (failure)
- Authorization: Required ([Authorize])
- Source: SC.Api/Controllers/OrdersController.cs

---

## GET /api/dishes

- Controller: SC.Api.Controllers.DishesController
- Action: GetAllDishes
- RequestType: GetAllDishesQuery (pagination + filters)
- ResponseType: Result<PaginatedList<GetAllDishesResponse>>

### Request Query Parameters

```
pageNumber: int (default: 1)
pageSize: int (default: 10)
[Additional filters unknown - see source]
```

### Notes

- Status codes: 200 (success), 400 (failure)
- Authorization: None explicitly required
- Source: SC.Api/Controllers/DishesController.cs

---

## GET /api/dishes/{id}

- Controller: SC.Api.Controllers.DishesController
- Action: GetDishById
- RequestType: Guid (route parameter)
- ResponseType: Result<GetDishByIdResponse>

### Notes

- Status codes: 200 (success), 404 (not found)
- Authorization: None explicitly required
- Source: SC.Api/Controllers/DishesController.cs

---

## POST /api/dishes

- Controller: SC.Api.Controllers.DishesController
- Action: CreateDish
- RequestType: CreateDishCommand
- ResponseType: Result<CreateDishResponse>

### Notes

- Status codes: 201 (created), 400 (failure)
- Authorization: None explicitly required
- Source: SC.Api/Controllers/DishesController.cs

---

## PUT /api/dishes/{id}

- Controller: SC.Api.Controllers.DishesController
- Action: UpdateDish
- RequestType: UpdateDishCommand
- ResponseType: Result<UpdateDishResponse>

### Notes

- Status codes: 200 (success), 400 (failure)
- Authorization: None explicitly required
- Source: SC.Api/Controllers/DishesController.cs

---

## PATCH /api/dishes/{id}/stock

- Controller: SC.Api.Controllers.DishesController
- Action: UpdateDishStock
- RequestType: UpdateDishStockCommand
- ResponseType: Result<UpdateDishStockResponse>

### Notes

- Status codes: 200 (success), 400 (failure)
- Authorization: None explicitly required
- Source: SC.Api/Controllers/DishesController.cs

---

## DELETE /api/dishes/{id}

- Controller: SC.Api.Controllers.DishesController
- Action: DeleteDish
- RequestType: Guid (route parameter)
- ResponseType: Result<DeleteDishResponse>

### Notes

- Status codes: 200 (success), 404 (not found)
- Authorization: None explicitly required
- Source: SC.Api/Controllers/DishesController.cs

---

## GET /api/settings

- Controller: SC.Api.Controllers.SettingsController
- Action: GetAllSettings
- RequestType: GetAllSettingsQuery
- ResponseType: Result<PaginatedList<GetAllSettingsResponse>>

### Notes

- Status codes: 200 (success), 400 (failure)
- Authorization: None explicitly required
- Source: SC.Api/Controllers/SettingsController.cs

---

## GET /api/settings/{id}

- Controller: SC.Api.Controllers.SettingsController
- Action: GetSettingById
- RequestType: Guid
- ResponseType: Result<GetSettingByIdResponse>

### Notes

- Status codes: 200 (success), 404 (not found)
- Authorization: None explicitly required
- Source: SC.Api/Controllers/SettingsController.cs

---

## POST /api/settings

- Controller: SC.Api.Controllers.SettingsController
- Action: CreateSetting
- RequestType: CreateSettingCommand
- ResponseType: Result<CreateSettingResponse>

### Notes

- Status codes: 201 (created), 400 (failure)
- Authorization: None explicitly required
- Source: SC.Api/Controllers/SettingsController.cs

---

## PUT /api/settings/{id}

- Controller: SC.Api.Controllers.SettingsController
- Action: UpdateSetting
- RequestType: UpdateSettingCommand
- ResponseType: Result<UpdateSettingResponse>

### Notes

- Status codes: 200 (success), 400 (failure)
- Authorization: None explicitly required
- Source: SC.Api/Controllers/SettingsController.cs

---

## DELETE /api/settings/{id}

- Controller: SC.Api.Controllers.SettingsController
- Action: DeleteSetting
- RequestType: Guid
- ResponseType: Result<DeleteSettingResponse>

### Notes

- Status codes: 200 (success), 404 (not found)
- Authorization: None explicitly required
- Source: SC.Api/Controllers/SettingsController.cs

---

## POST /api/auth/register

- Controller: SC.Api.Controllers.AuthController
- Action: Register
- RequestType: RegisterUserCommand
- ResponseType: Result<RegisterUserResponse>

### Notes

- Status codes: 201 (created), 400 (validation), 409 (conflict - email exists)
- Authorization: None required ([AllowAnonymous])
- Source: SC.Api/Controllers/AuthController.cs

---

## POST /api/auth/login

- Controller: SC.Api.Controllers.AuthController
- Action: Login
- RequestType: LoginCommand
- ResponseType: Result<LoginResponse>

### Notes

- Status codes: 200 (success), 400 (failure), 401 (invalid credentials)
- Authorization: None required ([AllowAnonymous])
- Source: SC.Api/Controllers/AuthController.cs

---

## POST /api/auth/refresh

- Controller: SC.Api.Controllers.AuthController
- Action: Refresh
- RequestType: RefreshTokenCommand
- ResponseType: Result<RefreshTokenResponse>

### Notes

- Status codes: 200 (success), 400 (failure), 401 (invalid token)
- Authorization: None required ([AllowAnonymous])
- Source: SC.Api/Controllers/AuthController.cs

---

## GET /api/auth/verify-email

- Controller: SC.Api.Controllers.AuthController
- Action: VerifyEmail
- RequestType: string (token query parameter)
- ResponseType: Result

### Request Query Parameters

```
token: string (email verification token)
```

### Notes

- Status codes: 200 (success), 400 (invalid token)
- Authorization: None required ([AllowAnonymous])
- Source: SC.Api/Controllers/AuthController.cs

---

## POST /api/auth/logout

- Controller: SC.Api.Controllers.AuthController
- Action: Logout
- RequestType: LogoutCommand
- ResponseType: Result

### Notes

- Status codes: 200 (success), 400 (failure)
- Authorization: Required ([Authorize])
- Source: SC.Api/Controllers/AuthController.cs

---

## GET /api/auth/me

- Controller: SC.Api.Controllers.AuthController
- Action: Me
- RequestType: none (GetCurrentUserQuery sent internally)
- ResponseType: Result<CurrentUserResponse>

### Notes

- Status codes: 200 (success), 400 (failure)
- Authorization: Required ([Authorize])
- Source: SC.Api/Controllers/AuthController.cs

---

## POST /api/verification/submit

- Controller: SC.Api.Controllers.VerificationController
- Action: Submit
- RequestType: FormData (multipart/form-data with files + documentTypes)
- ResponseType: Result<SubmitVerificationResponse>

### Request Multipart Form Fields

```
files: List<IFormFile> (at least one file required)
documentTypes: List<DocumentType> (must match file count)
  - 1: StudentCard
  - 2: NationalId
  - 3: Other
```

### Notes

- Status codes: 201 (created), 400 (validation), 413 (file too large), 403 (account inactive)
- Authorization: Required ([Authorize])
- Source: SC.Api/Controllers/VerificationController.cs

---

## GET /api/verification/me

- Controller: SC.Api.Controllers.VerificationController
- Action: GetMyStatus
- RequestType: none
- ResponseType: Result<GetMyVerificationStatusResponse>

### Notes

- Status codes: 200 (success), 400 (failure)
- Authorization: Required ([Authorize])
- Source: SC.Api/Controllers/VerificationController.cs

---

## GET /api/admin/verifications

- Controller: SC.Api.Controllers.Admin.VerificationAdminController
- Action: List
- RequestType: ListPendingVerificationsQuery
- ResponseType: Result<PaginatedList<VerificationListItemResponse>>

### Notes

- Status codes: 200 (success), 400 (failure)
- Authorization: Required with Admin role
- Source: SC.Api/Controllers/Admin/VerificationAdminController.cs

---

## GET /api/admin/verifications/{id}

- Controller: SC.Api.Controllers.Admin.VerificationAdminController
- Action: GetDetail
- RequestType: Guid
- ResponseType: Result<GetVerificationDetailResponse>

### Notes

- Status codes: 200 (success), 404 (not found)
- Authorization: Required with Admin role
- Source: SC.Api/Controllers/Admin/VerificationAdminController.cs

---

## POST /api/admin/verifications/{id}/approve

- Controller: SC.Api.Controllers.Admin.VerificationAdminController
- Action: Approve
- RequestType: Guid (in route)
- ResponseType: Result

### Notes

- Status codes: 200 (success), 404 (not found), 409 (not pending)
- Authorization: Required with Admin role
- Source: SC.Api/Controllers/Admin/VerificationAdminController.cs

---

## POST /api/admin/verifications/{id}/reject

- Controller: SC.Api.Controllers.Admin.VerificationAdminController
- Action: Reject
- RequestType: Guid (route) + RejectRequestBody
- ResponseType: Result

### Request JSON

```json
{
  "reason": "Documents are unclear"
}
```

### Notes

- Status codes: 200 (success), 404 (not found), 409 (not pending), 400 (missing reason)
- Authorization: Required with Admin role
- Source: SC.Api/Controllers/Admin/VerificationAdminController.cs

---

## Summary

**Total Endpoints**: 44
**Base URL**: `api/` (v1.0)
**Authentication**:

- Some endpoints allow anonymous access
- Protected endpoints require Bearer token ([Authorize])
- Admin endpoints require Admin role
- All timestamps are in UTC ISO format

**Key Features**:

- Order creation automatically deducts from user wallet
- Pagination support with pageNumber and pageSize
- Flexible filtering on list endpoints
- Multipart form data support for file uploads
- Comprehensive error responses with error codes
