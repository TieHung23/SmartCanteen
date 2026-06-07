using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletTransactionAndDropBalanceSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BalanceSnapshot_BalanceAfter",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "BalanceSnapshot_BalanceBefore",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "BalanceSnapshot_DeltaAmount",
                table: "Payments");

            migrationBuilder.CreateTable(
                name: "WalletTransaction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransaction", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransaction_UserId",
                table: "WalletTransaction",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WalletTransaction");

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceSnapshot_BalanceAfter",
                table: "Payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceSnapshot_BalanceBefore",
                table: "Payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceSnapshot_DeltaAmount",
                table: "Payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
