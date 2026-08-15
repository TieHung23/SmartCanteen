using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Existing accounts on the lecturer mail domain keep their real category.
            migrationBuilder.Sql(
                """
                UPDATE "Users"
                SET "Category" = 2
                WHERE lower("Email") LIKE '%@fe.edu.vn';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Users");
        }
    }
}
