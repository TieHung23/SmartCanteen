# Plan: Implement Category Pagination, Seeding & Search

## Overview
Implement data seeding for 20 categories, add pagination support to GetAllCategories query using `IQuery<T>` pattern, and enable search filtering by Category Name. The implementation will follow the existing Clean Architecture + CQRS (MediatR) pattern with `Result<T>` response wrappers.

## Current State Analysis

### Existing Architecture
- **Pattern**: Clean Architecture with CQRS (MediatR)
- **Project Structure**:
  - `SC.Api` - Controllers layer
  - `SC.Application` - MediatR handlers and DTOs
  - `SC.Domain` - Domain entities (Category aggregate root)
  - `SC.Persistence` - Database context and repository implementation
  - `SC.Contract` - Shared abstractions and response wrappers

- **Key Components**:
  - `Category` entity: Has `Id` (Guid), `Name`, `Description`, `CreatedAtUtc`, `CreatedBy`, `UpdatedAtUtc`, `UpdatedBy`
  - `GetAllCategoriesQuery`: Currently a simple record with no parameters
  - `GetAllCategoriesResponse`: DTO with `Id`, `Name`, `Description`
  - `RepositoryImp<TEntity, TKey>`: Generic repository with `FindAll()` method returning `IQueryable<TEntity>`
  - `Result<TValue>`: Generic result wrapper for API responses

### Current Gaps
- No pagination mechanism in place
- No seeding data for categories
- Query handler not implemented for GetAllCategories
- Controller endpoint not implemented (throws exception)
- No support for search/filtering parameters

## Implementation Steps

### Step 1: Create Pagination Infrastructure
**Location**: `SC.Contract/Shared/`

#### 1.1 Create `PaginationParams.cs`
- Properties: `PageNumber` (default: 1), `PageSize` (default: 10), `SearchKey` (optional)
- Validation: PageNumber >= 1, PageSize between 1-100
- Default values for missing parameters

#### 1.2 Create `PaginatedList<T>.cs`
- Generic class to wrap paginated results
- Properties: `Items` (List<T>), `PageNumber`, `PageSize`, `TotalCount`, `TotalPages`
- Constructor calculates `TotalPages = Math.Ceiling(TotalCount / (decimal)PageSize)`
- Helper method to calculate if HasPreviousPage/HasNextPage

**Rationale**: Standardizes pagination response structure across the application

### Step 2: Create Category Seed Data
**Location**: `SC.Persistence/Database/`

#### 2.1 Modify `CategoryConfiguration.cs`
- Add `HasData()` in the `Configure()` method
- Seed exactly 20 categories with realistic data (e.g., Beverages, Desserts, Main Courses, etc.)
- Assign unique Guids and timestamps for each entry

**Alternative Approach**: Create separate `CategorySeeder.cs` class with static method that's called during migration or in `SmartCanteenDbContext.OnModelCreating()`

**Rationale**: Data seeding at configuration level ensures consistency across environments

### Step 3: Update GetAllCategories Query Pattern
**Location**: `SC.Application/MediatR/Category/GetAllCategories/`

#### 3.1 Modify `GetAllCategoriesQuery.cs`
```csharp
public record GetAllCategoriesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchKey = null
) : IQuery<PaginatedList<GetAllCategoriesResponse>>;
```

#### 3.2 Modify `GetAllCategoriesResponse.cs`
- Keep as simple DTO (no changes needed)
- Will be wrapped in `PaginatedList<GetAllCategoriesResponse>` return type

**Rationale**: Query record parameters match pagination requirements; return type now generic list wrapper

### Step 4: Implement Query Handler
**Location**: `SC.Application/MediatR/Category/GetAllCategories/`

#### 4.1 Create `GetAllCategoriesQueryHandler.cs`
- Implements `IQueryHandler<GetAllCategoriesQuery, PaginatedList<GetAllCategoriesResponse>>`
- Handle<GetAllCategoriesQuery>() implementation:
  1. Get all categories from repository using `FindAll()`
  2. Apply search filter if `SearchKey` provided (case-insensitive Name match)
  3. Get total count before pagination
  4. Apply `Skip()` and `Take()` for pagination
  5. Map to `GetAllCategoriesResponse` DTOs
  6. Return `new PaginatedList<GetAllCategoriesResponse>(...)`

**Considerations**:
- Use `ToListAsync()` to execute queries
- Implement search as: `x => x.Name.Contains(SearchKey, StringComparison.OrdinalIgnoreCase)`
- Calculate skip amount: `(PageNumber - 1) * PageSize`

**Rationale**: Separates query logic from controller; maintains CQRS pattern

### Step 5: Update API Controller
**Location**: `SC.Api/Controllers/CategoriesController.cs`

#### 5.1 Modify `GetAllCategories()` endpoint
```csharp
[HttpGet]
public async Task<IActionResult> GetAllCategories(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? searchKey = null
)
{
    var query = new GetAllCategoriesQuery(pageNumber, pageSize, searchKey);
    var result = await mediator.Send(query);
    
    if (result.IsFailure)
        return BadRequest(result);
    
    return Ok(result);
}
```

**Rationale**: Maps HTTP query parameters to MediatR query pattern

### Step 6: Database Migration
**Location**: `SC.Persistence/Database/Migrations/`

#### 6.1 Create EF Core migration
- Run: `dotnet ef migrations add AddCategorySeeding --project SC.Persistence`
- Verify migration includes seeded categories
- Run: `dotnet ef database update` to apply

## Testing Strategy

### Test Scenarios
1. **Default pagination**: `GET /api/categories` → Page 1, 10 items
2. **Custom pagination**: `GET /api/categories?pageNumber=2&pageSize=5` → Page 2, 5 items
3. **Search filtering**: `GET /api/categories?searchKey=Beverage` → Categories matching name
4. **Combined**: `GET /api/categories?pageNumber=1&pageSize=10&searchKey=Drink` → First 10 filtered items
5. **Edge cases**: 
   - Empty search results
   - Page number exceeding total pages
   - Very large page size
   - Invalid/negative page numbers (should default or validate)

### Validation Rules
- PageNumber minimum: 1
- PageSize minimum: 1, maximum: 100
- SearchKey: optional, case-insensitive matching

## Deliverables

### Files to Create
1. `SC.Contract/Shared/PaginationParams.cs`
2. `SC.Contract/Shared/PaginatedList.cs`
3. `SC.Application/MediatR/Category/GetAllCategories/GetAllCategoriesQueryHandler.cs`
4. EF Core migration file (auto-generated)

### Files to Modify
1. `SC.Persistence/Database/Configuration/CategoryConfiguration.cs` - Add seed data
2. `SC.Application/MediatR/Category/GetAllCategories/GetAllCategoriesQuery.cs` - Add parameters
3. `SC.Api/Controllers/CategoriesController.cs` - Implement endpoint

### No Changes Required
- `GetAllCategoriesResponse.cs` - Remains as-is
- `SmartCanteenDbContext.cs` - No direct changes (migration handles schema)
- `RepositoryImp.cs` - Already supports required query patterns

## Implementation Considerations

### Design Decisions
1. **Seeding Location**: Use EntityFramework `HasData()` in configuration for maintainability
2. **Search Scope**: Name field only (can extend to Description later if needed)
3. **Default Pagination**: Page 1, 10 items per page
4. **Error Handling**: Invalid pagination parameters should use defaults + validation messages
5. **Case Sensitivity**: Search filtering is case-insensitive for better UX

### Future Enhancements
- Sort capabilities (OrderBy, OrderByDescending)
- Multi-field search (Name + Description)
- Custom page size limits per client tier
- Caching layer for frequently accessed categories
- Soft delete support for categories

## Dependencies & Assumptions
- MediatR v12+ (for IQueryHandler)
- EntityFramework Core 8+ (for HasData and LINQ support)
- Existing `Result<T>` pattern usage throughout API
- PostgreSQL database (inferred from connection string and migrations)
- Repository pattern already configured with dependency injection

## Risk Assessment
- **Low Risk**: Adding new parameters to query record (backward compatible with defaults)
- **Medium Risk**: Database migration must execute successfully (test in dev first)
- **Low Risk**: New handler implementation (follows established patterns)
- **Low Risk**: Pagination logic (standard skip/take pattern)

## Success Criteria
✓ GetAllCategories endpoint returns paginated results (not all 20 items at once)
✓ Search by Name filter works case-insensitively
✓ Pagination metadata included in response (PageNumber, PageSize, TotalCount, TotalPages)
✓ 20 seed categories present in database after migration
✓ Default pagination (page=1, size=10) returns first 10 items
✓ Invalid page numbers handled gracefully with validation
✓ Swagger documentation reflects query parameters

