using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    public partial class AddRefundRequestProposalContext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderItemId",
                table: "RefundRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChangeProposalId",
                table: "RefundRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DishId",
                table: "RefundRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_OrderItemId",
                table: "RefundRequests",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_ChangeProposalId",
                table: "RefundRequests",
                column: "ChangeProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_DishId",
                table: "RefundRequests",
                column: "DishId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefundRequests_OrderItemId",
                table: "RefundRequests");

            migrationBuilder.DropIndex(
                name: "IX_RefundRequests_ChangeProposalId",
                table: "RefundRequests");

            migrationBuilder.DropIndex(
                name: "IX_RefundRequests_DishId",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "ChangeProposalId",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "DishId",
                table: "RefundRequests");
        }
    }
}
