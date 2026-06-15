using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMealTemplateToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MealTemplateId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_MealTemplateId",
                table: "Orders",
                column: "MealTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_MealTemplate_MealTemplateId",
                table: "Orders",
                column: "MealTemplateId",
                principalTable: "MealTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_MealTemplate_MealTemplateId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_MealTemplateId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "MealTemplateId",
                table: "Orders");
        }
    }
}
