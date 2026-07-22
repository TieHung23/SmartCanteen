using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class DropPickupSlotTrayId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrayId",
                table: "PickupSlots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TrayId",
                table: "PickupSlots",
                type: "uuid",
                nullable: true);
        }
    }
}
