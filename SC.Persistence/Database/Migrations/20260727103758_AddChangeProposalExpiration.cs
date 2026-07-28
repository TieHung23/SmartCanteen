using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddChangeProposalExpiration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAtUtc",
                table: "OrderItemChangeProposals",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.Sql(
                """
                UPDATE "OrderItemChangeProposals"
                SET "ExpiresAtUtc" = "CreatedAtUtc" + INTERVAL '30 minutes'
                WHERE "ExpiresAtUtc" = TIMESTAMPTZ '0001-01-01 00:00:00+00';

                INSERT INTO "Settings"
                    ("Id", "Code", "Name", "Description", "Group", "Scope", "Value", "Type", "IsDeleted", "DeletedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
                SELECT
                    '33333333-3333-4333-8333-333333333331',
                    'RESPONSE_WINDOW_MINUTES',
                    'Change proposal response window',
                    'Minutes a customer has to respond to a change proposal before automatic refund handling.',
                    'CHANGE_PROPOSAL',
                    'RESPONSE',
                    '30',
                    'int',
                    false,
                    NULL,
                    NOW(),
                    '00000000-0000-0000-0000-000000000000',
                    NULL,
                    '00000000-0000-0000-0000-000000000000'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Settings"
                    WHERE "Group" = 'CHANGE_PROPOSAL'
                      AND "Scope" = 'RESPONSE'
                      AND "Code" = 'RESPONSE_WINDOW_MINUTES'
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
                WHERE "Id" = '33333333-3333-4333-8333-333333333331';
                """);

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "OrderItemChangeProposals");
        }
    }
}
