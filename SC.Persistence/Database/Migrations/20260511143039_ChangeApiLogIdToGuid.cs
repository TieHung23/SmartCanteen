using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class ChangeApiLogIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationLogs");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.RenameColumn(
                name: "BalanceCurrency",
                table: "Users",
                newName: "Balance_Currency");

            migrationBuilder.RenameColumn(
                name: "BalanceAmount",
                table: "Users",
                newName: "Balance_Amount");

            migrationBuilder.RenameColumn(
                name: "Key",
                table: "Settings",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "BalanceSnapshotDeltaAmount",
                table: "Payments",
                newName: "BalanceSnapshot_DeltaAmount");

            migrationBuilder.RenameColumn(
                name: "BalanceSnapshotBalanceBefore",
                table: "Payments",
                newName: "BalanceSnapshot_BalanceBefore");

            migrationBuilder.RenameColumn(
                name: "BalanceSnapshotBalanceAfter",
                table: "Payments",
                newName: "BalanceSnapshot_BalanceAfter");

            migrationBuilder.RenameColumn(
                name: "PriceCurrency",
                table: "Meals",
                newName: "Price_Currency");

            migrationBuilder.RenameColumn(
                name: "PriceAmount",
                table: "Meals",
                newName: "Price_Amount");

            migrationBuilder.RenameColumn(
                name: "PriceCurrency",
                table: "Dishes",
                newName: "Price_Currency");

            migrationBuilder.RenameColumn(
                name: "PriceAmount",
                table: "Dishes",
                newName: "Price_Amount");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Settings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "Settings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Settings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Settings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "Settings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Settings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                table: "Settings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Settings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ApiLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginId = table.Column<string>(type: "text", nullable: true),
                    LogLevel = table.Column<string>(type: "text", nullable: false),
                    ApiUrl = table.Column<string>(type: "text", nullable: false),
                    ApiMethod = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true),
                    ErrorTrace = table.Column<string>(type: "text", nullable: true),
                    ApiBody = table.Column<string>(type: "text", nullable: true),
                    ApiResponse = table.Column<string>(type: "text", nullable: true),
                    LocalIpAddress = table.Column<string>(type: "text", nullable: true),
                    LocalHostPC = table.Column<string>(type: "text", nullable: true),
                    LogApp = table.Column<string>(type: "text", nullable: true),
                    LogVersion = table.Column<string>(type: "text", nullable: true),
                    Memo = table.Column<string>(type: "text", nullable: true),
                    RequestId = table.Column<string>(type: "text", nullable: true),
                    ApiDesc = table.Column<string>(type: "text", nullable: true),
                    ApiVer = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItem",
                columns: table => new
                {
                    DishId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice_Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPrice_Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItem", x => new { x.OrderId, x.DishId });
                    table.ForeignKey(
                        name: "FK_OrderItem_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiLogs");

            migrationBuilder.DropTable(
                name: "OrderItem");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "Group",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Settings");

            migrationBuilder.RenameColumn(
                name: "Balance_Currency",
                table: "Users",
                newName: "BalanceCurrency");

            migrationBuilder.RenameColumn(
                name: "Balance_Amount",
                table: "Users",
                newName: "BalanceAmount");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Settings",
                newName: "Key");

            migrationBuilder.RenameColumn(
                name: "BalanceSnapshot_DeltaAmount",
                table: "Payments",
                newName: "BalanceSnapshotDeltaAmount");

            migrationBuilder.RenameColumn(
                name: "BalanceSnapshot_BalanceBefore",
                table: "Payments",
                newName: "BalanceSnapshotBalanceBefore");

            migrationBuilder.RenameColumn(
                name: "BalanceSnapshot_BalanceAfter",
                table: "Payments",
                newName: "BalanceSnapshotBalanceAfter");

            migrationBuilder.RenameColumn(
                name: "Price_Currency",
                table: "Meals",
                newName: "PriceCurrency");

            migrationBuilder.RenameColumn(
                name: "Price_Amount",
                table: "Meals",
                newName: "PriceAmount");

            migrationBuilder.RenameColumn(
                name: "Price_Currency",
                table: "Dishes",
                newName: "PriceCurrency");

            migrationBuilder.RenameColumn(
                name: "Price_Amount",
                table: "Dishes",
                newName: "PriceAmount");

            migrationBuilder.CreateTable(
                name: "ApplicationLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Exception = table.Column<string>(type: "text", nullable: true),
                    HttpMethod = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    MessageTemplate = table.Column<string>(type: "text", nullable: false),
                    Properties = table.Column<string>(type: "jsonb", nullable: true),
                    RequestPath = table.Column<string>(type: "text", nullable: true),
                    TimeStamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    DishId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPriceCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => new { x.OrderId, x.DishId });
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
