using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddChangeProposalResolutionRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRequiredItem",
                table: "OrderItemChangeProposals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RequiredCategoryId",
                table: "OrderItemChangeProposals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RespondedAtUtc",
                table: "OrderItemChangeProposals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SelectedDishId",
                table: "OrderItemChangeProposals",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRequiredItem",
                table: "OrderItemChangeProposals");

            migrationBuilder.DropColumn(
                name: "RequiredCategoryId",
                table: "OrderItemChangeProposals");

            migrationBuilder.DropColumn(
                name: "RespondedAtUtc",
                table: "OrderItemChangeProposals");

            migrationBuilder.DropColumn(
                name: "SelectedDishId",
                table: "OrderItemChangeProposals");
        }
    }
}
