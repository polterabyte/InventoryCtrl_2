using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveManufacturerFromProductModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductModels_Manufacturers_ManufacturerId",
                table: "ProductModels");

            migrationBuilder.DropIndex(
                name: "IX_ProductModels_ManufacturerId",
                table: "ProductModels");

            migrationBuilder.DropColumn(
                name: "ManufacturerId",
                table: "ProductModels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ManufacturerId",
                table: "ProductModels",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProductModels_ManufacturerId",
                table: "ProductModels",
                column: "ManufacturerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductModels_Manufacturers_ManufacturerId",
                table: "ProductModels",
                column: "ManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}