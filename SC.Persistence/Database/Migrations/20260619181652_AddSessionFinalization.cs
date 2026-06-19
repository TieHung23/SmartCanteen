using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionFinalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutoFinalizePolicy",
                table: "Sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FinalizationDeadline",
                table: "Sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FinalizedAtUtc",
                table: "Sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinalized",
                table: "Sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PreparedQuantity",
                table: "SessionDish",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemStatus",
                table: "OrderItem",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OrderItemChangeProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentDishId = table.Column<Guid>(type: "uuid", nullable: false),
                    SuggestedDishId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProposalStatus = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemChangeProposals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemChangeProposals_OrderId",
                table: "OrderItemChangeProposals",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemChangeProposals_UserId",
                table: "OrderItemChangeProposals",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemChangeProposals");

            migrationBuilder.DropColumn(
                name: "AutoFinalizePolicy",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "FinalizationDeadline",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "FinalizedAtUtc",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "IsFinalized",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "PreparedQuantity",
                table: "SessionDish");

            migrationBuilder.DropColumn(
                name: "ItemStatus",
                table: "OrderItem");
        }
    }
}
