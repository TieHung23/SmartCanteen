using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations;

[DbContext(typeof(SmartCanteenDbContext))]
[Migration("20260608070000_AddPaymentIdempotencyConstraints")]
public partial class AddPaymentIdempotencyConstraints : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_WalletTransaction_PaymentId",
            table: "WalletTransaction",
            column: "PaymentId",
            unique: true,
            filter: "\"PaymentId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_GatewayTransactionId",
            table: "Payments",
            column: "GatewayTransactionId",
            unique: true,
            filter: "\"GatewayTransactionId\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_WalletTransaction_PaymentId",
            table: "WalletTransaction");

        migrationBuilder.DropIndex(
            name: "IX_Payments_GatewayTransactionId",
            table: "Payments");
    }
}
