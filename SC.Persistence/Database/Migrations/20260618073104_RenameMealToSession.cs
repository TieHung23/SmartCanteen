using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameMealToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DishMeal_Dishes_DishId",
                table: "DishMeal");

            migrationBuilder.DropForeignKey(
                name: "FK_DishMeal_Meals_MealId",
                table: "DishMeal");

            migrationBuilder.DropForeignKey(
                name: "FK_MealTemplate_Meals_MealId",
                table: "MealTemplate");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Meals_MealId",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DishMeal",
                table: "DishMeal");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Meals",
                table: "Meals");

            migrationBuilder.RenameTable(
                name: "Meals",
                newName: "Sessions");

            migrationBuilder.RenameTable(
                name: "DishMeal",
                newName: "SessionDish");

            migrationBuilder.RenameColumn(
                name: "MealId",
                table: "Orders",
                newName: "SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_MealId",
                table: "Orders",
                newName: "IX_Orders_SessionId");

            migrationBuilder.RenameColumn(
                name: "MealId",
                table: "MealTemplate",
                newName: "SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_MealTemplate_MealId",
                table: "MealTemplate",
                newName: "IX_MealTemplate_SessionId");

            migrationBuilder.RenameColumn(
                name: "MealId",
                table: "SessionDish",
                newName: "SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_DishMeal_MealId",
                table: "SessionDish",
                newName: "IX_SessionDish_SessionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sessions",
                table: "Sessions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SessionDish",
                table: "SessionDish",
                columns: new[] { "DishId", "SessionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_MealTemplate_Sessions_SessionId",
                table: "MealTemplate",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Sessions_SessionId",
                table: "Orders",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionDish_Dishes_DishId",
                table: "SessionDish",
                column: "DishId",
                principalTable: "Dishes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionDish_Sessions_SessionId",
                table: "SessionDish",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(
                """
                UPDATE "Carts"
                SET "DataJson" = replace(
                    replace("DataJson"::text, '"mealId"', '"sessionId"'),
                    '"meals"',
                    '"sessions"')::jsonb
                WHERE "DataJson" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealTemplate_Sessions_SessionId",
                table: "MealTemplate");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Sessions_SessionId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionDish_Dishes_DishId",
                table: "SessionDish");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionDish_Sessions_SessionId",
                table: "SessionDish");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SessionDish",
                table: "SessionDish");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sessions",
                table: "Sessions");

            migrationBuilder.RenameTable(
                name: "Sessions",
                newName: "Meals");

            migrationBuilder.RenameTable(
                name: "SessionDish",
                newName: "DishMeal");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "Orders",
                newName: "MealId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_SessionId",
                table: "Orders",
                newName: "IX_Orders_MealId");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "MealTemplate",
                newName: "MealId");

            migrationBuilder.RenameIndex(
                name: "IX_MealTemplate_SessionId",
                table: "MealTemplate",
                newName: "IX_MealTemplate_MealId");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "DishMeal",
                newName: "MealId");

            migrationBuilder.RenameIndex(
                name: "IX_SessionDish_SessionId",
                table: "DishMeal",
                newName: "IX_DishMeal_MealId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Meals",
                table: "Meals",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DishMeal",
                table: "DishMeal",
                columns: new[] { "DishId", "MealId" });

            migrationBuilder.AddForeignKey(
                name: "FK_DishMeal_Dishes_DishId",
                table: "DishMeal",
                column: "DishId",
                principalTable: "Dishes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DishMeal_Meals_MealId",
                table: "DishMeal",
                column: "MealId",
                principalTable: "Meals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MealTemplate_Meals_MealId",
                table: "MealTemplate",
                column: "MealId",
                principalTable: "Meals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Meals_MealId",
                table: "Orders",
                column: "MealId",
                principalTable: "Meals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(
                """
                UPDATE "Carts"
                SET "DataJson" = replace(
                    replace("DataJson"::text, '"sessionId"', '"mealId"'),
                    '"sessions"',
                    '"meals"')::jsonb
                WHERE "DataJson" IS NOT NULL;
                """);
        }
    }
}
