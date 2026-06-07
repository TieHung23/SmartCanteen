using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePaymentIdWithWalletTransactionIdInOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PaymentId",
                table: "Orders",
                newName: "WalletTransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_PaymentId",
                table: "Orders",
                newName: "IX_Orders_WalletTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WalletTransactionId",
                table: "Orders",
                newName: "PaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_WalletTransactionId",
                table: "Orders",
                newName: "IX_Orders_PaymentId");
        }
    }
}
