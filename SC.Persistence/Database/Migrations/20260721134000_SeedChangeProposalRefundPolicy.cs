using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeedChangeProposalRefundPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "Settings"
                WHERE "Id" IN (
                    '11111111-1111-4111-8111-111111111111',
                    '11111111-1111-4111-8111-111111111112',
                    '11111111-1111-4111-8111-111111111113',
                    '11111111-1111-4111-8111-111111111114',
                    '11111111-1111-4111-8111-111111111115'
                );
                """);
        }
    }
}
