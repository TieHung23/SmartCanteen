using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDishMealMealTemplateAndCategoryImgUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealSettings_Meals_MealId",
                table: "MealSettings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MealSettings",
                table: "MealSettings");

            migrationBuilder.DropIndex(
                name: "IX_Dishes_MealId",
                table: "Dishes");

            migrationBuilder.DropColumn(
                name: "MealId",
                table: "Dishes");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "Dishes");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "MealSettings",
                newName: "MinQuantity");

            migrationBuilder.RenameColumn(
                name: "MealId",
                table: "MealSettings",
                newName: "UpdatedBy");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "MealSettings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "MealSettings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "MealSettings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "MealSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxQuantity",
                table: "MealSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "MealTemplateId",
                table: "MealSettings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                table: "MealSettings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImgUrl",
                table: "Categories",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MealSettings",
                table: "MealSettings",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "DishMeal",
                columns: table => new
                {
                    DishId = table.Column<Guid>(type: "uuid", nullable: false),
                    MealId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DishMeal", x => new { x.DishId, x.MealId });
                    table.ForeignKey(
                        name: "FK_DishMeal_Dishes_DishId",
                        column: x => x.DishId,
                        principalTable: "Dishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DishMeal_Meals_MealId",
                        column: x => x.MealId,
                        principalTable: "Meals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MealId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealTemplate_Meals_MealId",
                        column: x => x.MealId,
                        principalTable: "Meals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MealSettings_CategoryId",
                table: "MealSettings",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MealSettings_MealTemplateId",
                table: "MealSettings",
                column: "MealTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_DishMeal_MealId",
                table: "DishMeal",
                column: "MealId");

            migrationBuilder.CreateIndex(
                name: "IX_MealTemplate_MealId",
                table: "MealTemplate",
                column: "MealId");

            migrationBuilder.AddForeignKey(
                name: "FK_MealSettings_MealTemplate_MealTemplateId",
                table: "MealSettings",
                column: "MealTemplateId",
                principalTable: "MealTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealSettings_MealTemplate_MealTemplateId",
                table: "MealSettings");

            migrationBuilder.DropTable(
                name: "DishMeal");

            migrationBuilder.DropTable(
                name: "MealTemplate");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MealSettings",
                table: "MealSettings");

            migrationBuilder.DropIndex(
                name: "IX_MealSettings_CategoryId",
                table: "MealSettings");

            migrationBuilder.DropIndex(
                name: "IX_MealSettings_MealTemplateId",
                table: "MealSettings");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "MealSettings");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "MealSettings");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "MealSettings");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "MealSettings");

            migrationBuilder.DropColumn(
                name: "MaxQuantity",
                table: "MealSettings");

            migrationBuilder.DropColumn(
                name: "MealTemplateId",
                table: "MealSettings");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "MealSettings");

            migrationBuilder.DropColumn(
                name: "ImgUrl",
                table: "Categories");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "MealSettings",
                newName: "MealId");

            migrationBuilder.RenameColumn(
                name: "MinQuantity",
                table: "MealSettings",
                newName: "Quantity");

            migrationBuilder.AddColumn<Guid>(
                name: "MealId",
                table: "Dishes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "Dishes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MealSettings",
                table: "MealSettings",
                columns: new[] { "MealId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Dishes_MealId",
                table: "Dishes",
                column: "MealId");

            migrationBuilder.AddForeignKey(
                name: "FK_MealSettings_Meals_MealId",
                table: "MealSettings",
                column: "MealId",
                principalTable: "Meals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
