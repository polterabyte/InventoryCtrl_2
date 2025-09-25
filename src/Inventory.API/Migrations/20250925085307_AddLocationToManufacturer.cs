using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationToManufacturer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Добавляем новые поля
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Manufacturers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactInfo",
                table: "Manufacturers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Manufacturers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Manufacturers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Сначала добавляем nullable поле LocationId
            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Manufacturers",
                type: "integer",
                nullable: true);

            // Создаем индекс
            migrationBuilder.CreateIndex(
                name: "IX_Manufacturers_LocationId",
                table: "Manufacturers",
                column: "LocationId");

            // Добавляем внешний ключ
            migrationBuilder.AddForeignKey(
                name: "FK_Manufacturers_Locations_LocationId",
                table: "Manufacturers",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Заполняем LocationId для существующих записей (берем первую доступную локацию)
            migrationBuilder.Sql(@"
                UPDATE ""Manufacturers"" 
                SET ""LocationId"" = (SELECT ""Id"" FROM ""Locations"" WHERE ""IsActive"" = true LIMIT 1)
                WHERE ""LocationId"" IS NULL;
            ");

            // Теперь делаем поле обязательным
            migrationBuilder.AlterColumn<int>(
                name: "LocationId",
                table: "Manufacturers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // Обновляем внешний ключ на CASCADE
            migrationBuilder.DropForeignKey(
                name: "FK_Manufacturers_Locations_LocationId",
                table: "Manufacturers");

            migrationBuilder.AddForeignKey(
                name: "FK_Manufacturers_Locations_LocationId",
                table: "Manufacturers",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Manufacturers_Locations_LocationId",
                table: "Manufacturers");

            migrationBuilder.DropIndex(
                name: "IX_Manufacturers_LocationId",
                table: "Manufacturers");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Manufacturers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Manufacturers");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Manufacturers");

            migrationBuilder.DropColumn(
                name: "ContactInfo",
                table: "Manufacturers");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Manufacturers");
        }
    }
}