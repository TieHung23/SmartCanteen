using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class DropRedundantRobotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trays_CurrentOrderId",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "CurrentOrderId",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "PayloadJson",
                table: "RobotEventLogs");

            migrationBuilder.DropColumn(
                name: "SensorOccupied",
                table: "PickupSlots");

            migrationBuilder.CreateIndex(
                name: "IX_ServingJobs_TrayId",
                table: "ServingJobs",
                column: "TrayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServingJobs_TrayId",
                table: "ServingJobs");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentOrderId",
                table: "Trays",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayloadJson",
                table: "RobotEventLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SensorOccupied",
                table: "PickupSlots",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trays_CurrentOrderId",
                table: "Trays",
                column: "CurrentOrderId");
        }
    }
}
