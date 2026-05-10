using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class DddRefactoring_SeparateMoneyAndRenaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dishes_Categories_CategoryId",
                table: "Dishes");

            migrationBuilder.DropForeignKey(
                name: "FK_Dishes_Meals_MealId",
                table: "Dishes");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Meals_MealId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Payments_PaymentId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_UserId",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "DishesId",
                table: "OrderItems",
                newName: "DishId");

            migrationBuilder.RenameColumn(
                name: "MoneyCurrency",
                table: "Meals",
                newName: "PriceCurrency");

            migrationBuilder.RenameColumn(
                name: "MoneyAmount",
                table: "Meals",
                newName: "PriceAmount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DishId",
                table: "OrderItems",
                newName: "DishesId");

            migrationBuilder.RenameColumn(
                name: "PriceCurrency",
                table: "Meals",
                newName: "MoneyCurrency");

            migrationBuilder.RenameColumn(
                name: "PriceAmount",
                table: "Meals",
                newName: "MoneyAmount");

            migrationBuilder.AddForeignKey(
                name: "FK_Dishes_Categories_CategoryId",
                table: "Dishes",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Dishes_Meals_MealId",
                table: "Dishes",
                column: "MealId",
                principalTable: "Meals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Meals_MealId",
                table: "Orders",
                column: "MealId",
                principalTable: "Meals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Payments_PaymentId",
                table: "Orders",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_UserId",
                table: "Payments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
