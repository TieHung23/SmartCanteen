using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    public partial class SeedChangeProposalItemRefundPolicy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "Settings"
                WHERE "Id" IN (
                    '22222222-2222-4222-8222-222222222221',
                    '22222222-2222-4222-8222-222222222222',
                    '22222222-2222-4222-8222-222222222223',
                    '22222222-2222-4222-8222-222222222224',
                    '22222222-2222-4222-8222-222222222225'
                );
                """);
        }
    }
}
