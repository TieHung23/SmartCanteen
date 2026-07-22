using Microsoft.EntityFrameworkCore;
using SC.Domain.Domain.Category.AggregateRoot;
using SC.Domain.Domain.Dish;
using SC.Domain.Domain.Dish.AggregateRoot;
using SC.Domain.Domain.Session.AggregateRoot;
using SC.Domain.Domain.Session.Entity;
using SC.Domain.Domain.Session.Enum;
using SC.Domain.Domain.User.Enum;
using SC.Domain.SharedKernel.ValueObjects;
using SC.Persistence.Database;

var db = new SmartCanteenDbContextFactory().CreateDbContext(args);
await db.Database.MigrateAsync();

if (args.Any(arg => arg.Equals("--ensure-change-proposal-schema", StringComparison.OrdinalIgnoreCase)))
{
    await EnsureChangeProposalSchemaAsync(db);
    Console.WriteLine("Ensured change proposal schema and refund policy settings.");
    return;
}

if (args.Any(arg => arg.Equals("--repair-approved-item-refunds", StringComparison.OrdinalIgnoreCase)))
{
    var affectedRows = await RepairApprovedItemRefundsAsync(db);
    Console.WriteLine($"Repaired approved item refunds: {affectedRows} order item(s).");
    return;
}

var managerId = await db.Users
    .Where(u => !u.IsDeleted && u.Role == Role.Manager)
    .Select(u => u.Id)
    .FirstOrDefaultAsync();

if (managerId == Guid.Empty)
{
    managerId = await db.Users
        .Where(u => !u.IsDeleted)
        .Select(u => u.Id)
        .FirstOrDefaultAsync();
}

var requiredGroup = await GetCategoryWithActiveDishesAsync(db, minDishCount: 2);
if (requiredGroup is null)
{
    var category = Category.Create(
        "SC Test Required Main",
        "Required category for change proposal testing.",
        managerId);

    var firstDish = Dish.Create(
        "SC Test Main A",
        "Primary required dish for proposal shortage testing.",
        Money.Create(25000),
        category.Id,
        managerId);

    var replacementDish = Dish.Create(
        "SC Test Main B",
        "Replacement required dish in the same category.",
        Money.Create(25000),
        category.Id,
        managerId);

    await db.Categories.AddAsync(category);
    await db.Dishes.AddRangeAsync(firstDish, replacementDish);
    await db.SaveChangesAsync();

    requiredGroup = new CategoryDishGroup(category.Id, category.Name, [firstDish, replacementDish]);
}

var optionalGroup = await GetCategoryWithActiveDishesAsync(
    db,
    minDishCount: 2,
    excludedCategoryId: requiredGroup.CategoryId);

if (optionalGroup is null)
{
    var category = Category.Create(
        "SC Test Optional Side",
        "Optional category for item refund testing.",
        managerId);

    var optionalDish = Dish.Create(
        "SC Test Optional A",
        "Optional dish for proposal item refund testing.",
        Money.Create(10000),
        category.Id,
        managerId);

    var optionalReplacementDish = Dish.Create(
        "SC Test Optional B",
        "Replacement optional dish in the same category.",
        Money.Create(10000),
        category.Id,
        managerId);

    await db.Categories.AddAsync(category);
    await db.Dishes.AddRangeAsync(optionalDish, optionalReplacementDish);
    await db.SaveChangesAsync();

    optionalGroup = new CategoryDishGroup(category.Id, category.Name, [optionalDish, optionalReplacementDish]);
}

var now = DateTimeOffset.UtcNow;
var session = Session.Create(
    $"SC Proposal Test {now:yyyyMMdd-HHmmss}",
    "Generated test session for required/optional change proposal flow.",
    now.AddDays(1),
    now.AddDays(2),
    now.AddMinutes(-5),
    managerId);

session.ConfigureFinalization(
    now.AddDays(2).AddHours(-1),
    AutoFinalizePolicy.AutoReject,
    managerId);

var template = MealTemplate.Create(session.Id, "Proposal Test Combo");
template.AddSetting(requiredGroup.CategoryId, minQuantity: 1, maxQuantity: 1, isRequired: true);
template.AddSetting(optionalGroup.CategoryId, minQuantity: 0, maxQuantity: 1, isRequired: false);
session.AddMealTemplate(template);

foreach (var dish in requiredGroup.Dishes.Take(2).Concat(optionalGroup.Dishes.Take(1)))
{
    session.AddSessionDish(SessionDish.Create(dish.Id, session.Id));
}

if (optionalGroup.Dishes.Count > 1)
{
    session.AddSessionDish(SessionDish.Create(optionalGroup.Dishes[1].Id, session.Id));
}

await db.Sessions.AddAsync(session);
await db.SaveChangesAsync();

Console.WriteLine("Created proposal test session");
Console.WriteLine($"SessionId: {session.Id}");
Console.WriteLine($"MealTemplateId: {template.Id}");
Console.WriteLine($"RequiredCategoryId: {requiredGroup.CategoryId} ({requiredGroup.CategoryName})");
Console.WriteLine($"RequiredDishToOrder: {requiredGroup.Dishes[0].Id} ({requiredGroup.Dishes[0].Name})");
Console.WriteLine($"RequiredDishReplacement: {requiredGroup.Dishes[1].Id} ({requiredGroup.Dishes[1].Name})");
Console.WriteLine($"OptionalCategoryId: {optionalGroup.CategoryId} ({optionalGroup.CategoryName})");
Console.WriteLine($"OptionalDishToOrder: {optionalGroup.Dishes[0].Id} ({optionalGroup.Dishes[0].Name})");
Console.WriteLine($"OptionalDishReplacement: {optionalGroup.Dishes[1].Id} ({optionalGroup.Dishes[1].Name})");
Console.WriteLine($"AvailableForOrder: {session.AvailableForOrder:O}");
Console.WriteLine($"AvailableFrom: {session.AvailableFrom:O}");
Console.WriteLine($"AvailableTo: {session.AvailableTo:O}");
Console.WriteLine($"FinalizationDeadline: {session.FinalizationDeadline:O}");

static async Task<CategoryDishGroup?> GetCategoryWithActiveDishesAsync(
    SmartCanteenDbContext db,
    int minDishCount,
    Guid? excludedCategoryId = null)
{
    var dishes = await db.Dishes
        .Where(d => !d.IsDeleted
                    && d.IsActive
                    && (!excludedCategoryId.HasValue || d.CategoryId != excludedCategoryId.Value))
        .OrderBy(d => d.Name)
        .ToListAsync();

    var candidate = dishes
        .GroupBy(d => d.CategoryId)
        .Where(group => group.Count() >= minDishCount)
        .OrderByDescending(group => group.Count())
        .FirstOrDefault();

    if (candidate is null)
        return null;

    var category = await db.Categories
        .Where(c => !c.IsDeleted && c.Id == candidate.Key)
        .Select(c => new { c.Id, c.Name })
        .FirstOrDefaultAsync();

    if (category is null)
        return null;

    return new CategoryDishGroup(category.Id, category.Name, candidate.ToList());
}

static async Task EnsureChangeProposalSchemaAsync(SmartCanteenDbContext db)
{
    await db.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE "OrderItemChangeProposals"
            ADD COLUMN IF NOT EXISTS "IsRequiredItem" boolean NOT NULL DEFAULT false;

        ALTER TABLE "OrderItemChangeProposals"
            ADD COLUMN IF NOT EXISTS "RequiredCategoryId" uuid NULL;

        ALTER TABLE "OrderItemChangeProposals"
            ADD COLUMN IF NOT EXISTS "RespondedAtUtc" timestamp with time zone NULL;

        ALTER TABLE "OrderItemChangeProposals"
            ADD COLUMN IF NOT EXISTS "SelectedDishId" uuid NULL;

        ALTER TABLE "RefundRequests"
            ADD COLUMN IF NOT EXISTS "OrderItemId" integer NULL;

        ALTER TABLE "RefundRequests"
            ADD COLUMN IF NOT EXISTS "ChangeProposalId" uuid NULL;

        ALTER TABLE "RefundRequests"
            ADD COLUMN IF NOT EXISTS "DishId" uuid NULL;

        CREATE INDEX IF NOT EXISTS "IX_RefundRequests_OrderItemId"
            ON "RefundRequests" ("OrderItemId");

        CREATE INDEX IF NOT EXISTS "IX_RefundRequests_ChangeProposalId"
            ON "RefundRequests" ("ChangeProposalId");

        CREATE INDEX IF NOT EXISTS "IX_RefundRequests_DishId"
            ON "RefundRequests" ("DishId");
        """);

    await db.Database.ExecuteSqlRawAsync(
        """
        INSERT INTO "Settings"
            ("Id", "Code", "Name", "Description", "Group", "Scope", "Value", "Type", "IsDeleted", "DeletedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
        SELECT
            '11111111-1111-4111-8111-111111111111',
            'NAME',
            'Policy name',
            'Display name of the refund policy.',
            'REFUND_POLICY',
            'FULL_REFUND_NO_IMAGE',
            'Full refund without image',
            'string',
            false,
            NULL,
            NOW(),
            '00000000-0000-0000-0000-000000000000',
            NULL,
            '00000000-0000-0000-0000-000000000000'
        WHERE NOT EXISTS (
            SELECT 1 FROM "Settings"
            WHERE "Group" = 'REFUND_POLICY'
              AND "Scope" = 'FULL_REFUND_NO_IMAGE'
              AND "Code" = 'NAME'
              AND "IsDeleted" = false
        );

        INSERT INTO "Settings"
            ("Id", "Code", "Name", "Description", "Group", "Scope", "Value", "Type", "IsDeleted", "DeletedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
        SELECT
            '11111111-1111-4111-8111-111111111112',
            'DESCRIPTION',
            'Policy description',
            'Description of the refund policy.',
            'REFUND_POLICY',
            'FULL_REFUND_NO_IMAGE',
            'Full refund when the canteen cannot fulfill the order after session finalization.',
            'string',
            false,
            NULL,
            NOW(),
            '00000000-0000-0000-0000-000000000000',
            NULL,
            '00000000-0000-0000-0000-000000000000'
        WHERE NOT EXISTS (
            SELECT 1 FROM "Settings"
            WHERE "Group" = 'REFUND_POLICY'
              AND "Scope" = 'FULL_REFUND_NO_IMAGE'
              AND "Code" = 'DESCRIPTION'
              AND "IsDeleted" = false
        );

        INSERT INTO "Settings"
            ("Id", "Code", "Name", "Description", "Group", "Scope", "Value", "Type", "IsDeleted", "DeletedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
        SELECT
            '11111111-1111-4111-8111-111111111113',
            'PERCENT',
            'Refund percent',
            'Refund percentage for this policy.',
            'REFUND_POLICY',
            'FULL_REFUND_NO_IMAGE',
            '100',
            'decimal',
            false,
            NULL,
            NOW(),
            '00000000-0000-0000-0000-000000000000',
            NULL,
            '00000000-0000-0000-0000-000000000000'
        WHERE NOT EXISTS (
            SELECT 1 FROM "Settings"
            WHERE "Group" = 'REFUND_POLICY'
              AND "Scope" = 'FULL_REFUND_NO_IMAGE'
              AND "Code" = 'PERCENT'
              AND "IsDeleted" = false
        );

        INSERT INTO "Settings"
            ("Id", "Code", "Name", "Description", "Group", "Scope", "Value", "Type", "IsDeleted", "DeletedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
        SELECT
            '11111111-1111-4111-8111-111111111114',
            'REQUIRES_IMAGE',
            'Requires image',
            'Whether evidence images are required.',
            'REFUND_POLICY',
            'FULL_REFUND_NO_IMAGE',
            'false',
            'bool',
            false,
            NULL,
            NOW(),
            '00000000-0000-0000-0000-000000000000',
            NULL,
            '00000000-0000-0000-0000-000000000000'
        WHERE NOT EXISTS (
            SELECT 1 FROM "Settings"
            WHERE "Group" = 'REFUND_POLICY'
              AND "Scope" = 'FULL_REFUND_NO_IMAGE'
              AND "Code" = 'REQUIRES_IMAGE'
              AND "IsDeleted" = false
        );

        INSERT INTO "Settings"
            ("Id", "Code", "Name", "Description", "Group", "Scope", "Value", "Type", "IsDeleted", "DeletedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
        SELECT
            '11111111-1111-4111-8111-111111111115',
            'ORDER_REFUND_POLICY_CODE',
            'Change proposal order refund policy',
            'Refund policy used when a user requests full order refund from a change proposal.',
            'CHANGE_PROPOSAL',
            'REFUND',
            'FULL_REFUND_NO_IMAGE',
            'string',
            false,
            NULL,
            NOW(),
            '00000000-0000-0000-0000-000000000000',
            NULL,
            '00000000-0000-0000-0000-000000000000'
        WHERE NOT EXISTS (
            SELECT 1 FROM "Settings"
            WHERE "Group" = 'CHANGE_PROPOSAL'
              AND "Scope" = 'REFUND'
              AND "Code" = 'ORDER_REFUND_POLICY_CODE'
              AND "IsDeleted" = false
        );

        INSERT INTO "Settings"
            ("Id", "Code", "Name", "Description", "Group", "Scope", "Value", "Type", "IsDeleted", "DeletedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
        SELECT
            '22222222-2222-4222-8222-222222222221',
            'NAME',
            'Policy name',
            'Display name of the refund policy.',
            'REFUND_POLICY',
            'PROPOSAL_ITEM_REFUND_NO_IMAGE',
            'Change proposal item refund without image',
            'string',
            false,
            NULL,
            NOW(),
            '00000000-0000-0000-0000-000000000000',
            NULL,
            '00000000-0000-0000-0000-000000000000'
        WHERE NOT EXISTS (
            SELECT 1 FROM "Settings"
            WHERE "Group" = 'REFUND_POLICY'
              AND "Scope" = 'PROPOSAL_ITEM_REFUND_NO_IMAGE'
              AND "Code" = 'NAME'
              AND "IsDeleted" = false
        );

        INSERT INTO "Settings"
            ("Id", "Code", "Name", "Description", "Group", "Scope", "Value", "Type", "IsDeleted", "DeletedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
        SELECT
            '22222222-2222-4222-8222-222222222222',
            'DESCRIPTION',
            'Policy description',
            'Description of the refund policy.',
            'REFUND_POLICY',
            'PROPOSAL_ITEM_REFUND_NO_IMAGE',
            'Full item refund when the canteen cannot fulfill an optional item after session finalization.',
            'string',
            false,
            NULL,
            NOW(),
            '00000000-0000-0000-0000-000000000000',
            NULL,
            '00000000-0000-0000-0000-000000000000'
        WHERE NOT EXISTS (
            SELECT 1 FROM "Settings"
            WHERE "Group" = 'REFUND_POLICY'
              AND "Scope" = 'PROPOSAL_ITEM_REFUND_NO_IMAGE'
              AND "Code" = 'DESCRIPTION'
              AND "IsDeleted" = false
        );

        INSERT INTO "Settings"
            ("Id", "Code", "Name", "Description", "Group", "Scope", "Value", "Type", "IsDeleted", "DeletedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
        SELECT
            '22222222-2222-4222-8222-222222222223',
            'PERCENT',
            'Refund percent',
            'Refund percentage for this policy.',
            'REFUND_POLICY',
            'PROPOSAL_ITEM_REFUND_NO_IMAGE',
            '100',
            'decimal',
            false,
            NULL,
            NOW(),
            '00000000-0000-0000-0000-000000000000',
            NULL,
            '00000000-0000-0000-0000-000000000000'
        WHERE NOT EXISTS (
            SELECT 1 FROM "Settings"
            WHERE "Group" = 'REFUND_POLICY'
              AND "Scope" = 'PROPOSAL_ITEM_REFUND_NO_IMAGE'
              AND "Code" = 'PERCENT'
              AND "IsDeleted" = false
        );

        INSERT INTO "Settings"
            ("Id", "Code", "Name", "Description", "Group", "Scope", "Value", "Type", "IsDeleted", "DeletedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
        SELECT
            '22222222-2222-4222-8222-222222222224',
            'REQUIRES_IMAGE',
            'Requires image',
            'Whether evidence images are required.',
            'REFUND_POLICY',
            'PROPOSAL_ITEM_REFUND_NO_IMAGE',
            'false',
            'bool',
            false,
            NULL,
            NOW(),
            '00000000-0000-0000-0000-000000000000',
            NULL,
            '00000000-0000-0000-0000-000000000000'
        WHERE NOT EXISTS (
            SELECT 1 FROM "Settings"
            WHERE "Group" = 'REFUND_POLICY'
              AND "Scope" = 'PROPOSAL_ITEM_REFUND_NO_IMAGE'
              AND "Code" = 'REQUIRES_IMAGE'
              AND "IsDeleted" = false
        );

        INSERT INTO "Settings"
            ("Id", "Code", "Name", "Description", "Group", "Scope", "Value", "Type", "IsDeleted", "DeletedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
        SELECT
            '22222222-2222-4222-8222-222222222225',
            'ITEM_REFUND_POLICY_CODE',
            'Change proposal item refund policy',
            'Refund policy used when a user requests optional item refund from a change proposal.',
            'CHANGE_PROPOSAL',
            'REFUND',
            'PROPOSAL_ITEM_REFUND_NO_IMAGE',
            'string',
            false,
            NULL,
            NOW(),
            '00000000-0000-0000-0000-000000000000',
            NULL,
            '00000000-0000-0000-0000-000000000000'
        WHERE NOT EXISTS (
            SELECT 1 FROM "Settings"
            WHERE "Group" = 'CHANGE_PROPOSAL'
              AND "Scope" = 'REFUND'
              AND "Code" = 'ITEM_REFUND_POLICY_CODE'
              AND "IsDeleted" = false
        );
        """);
}

static async Task<int> RepairApprovedItemRefundsAsync(SmartCanteenDbContext db)
{
    return await db.Database.ExecuteSqlRawAsync(
        """
        UPDATE "OrderItem" AS oi
        SET "ItemStatus" = 4
        FROM "RefundRequests" AS rr
        WHERE rr."OrderItemId" = oi."Id"
          AND rr."Status" = 2
          AND rr."OrderItemId" IS NOT NULL
          AND oi."ItemStatus" = 5;
        """);
}

internal sealed record CategoryDishGroup(Guid CategoryId, string CategoryName, List<Dish> Dishes);
