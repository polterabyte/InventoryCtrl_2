using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.API.Migrations
{
    /// <inheritdoc />
    public partial class Warehouse_LocationRef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Warehouses");

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Warehouses",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_LocationId",
                table: "Warehouses",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Locations_LocationId",
                table: "Warehouses",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Locations_LocationId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_LocationId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Warehouses");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Warehouses",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
