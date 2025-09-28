using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCriticalPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Critical performance indexes for InventoryTransactions
            migrationBuilder.Sql(@"
                CREATE INDEX IX_InventoryTransaction_ProductId_Date 
                    ON ""InventoryTransactions"" (""ProductId"", ""Date"");
            ");
            
            migrationBuilder.Sql(@"
                CREATE INDEX IX_InventoryTransaction_Type_Date 
                    ON ""InventoryTransactions"" (""Type"", ""Date"");
            ");
            
            migrationBuilder.Sql(@"
                CREATE INDEX IX_InventoryTransaction_WarehouseId_Type 
                    ON ""InventoryTransactions"" (""WarehouseId"", ""Type"");
            ");
            
            migrationBuilder.Sql(@"
                CREATE INDEX IX_InventoryTransaction_UserId_Date 
                    ON ""InventoryTransactions"" (""UserId"", ""Date"");
            ");
            
            migrationBuilder.Sql(@"
                CREATE INDEX IX_InventoryTransaction_RequestId 
                    ON ""InventoryTransactions"" (""RequestId"") WHERE ""RequestId"" IS NOT NULL;
            ");
            
            // Critical performance indexes for AuditLogs
            migrationBuilder.Sql(@"
                CREATE INDEX IX_AuditLog_EntityName_EntityId 
                    ON ""AuditLogs"" (""EntityName"", ""EntityId"");
            ");
            
            migrationBuilder.Sql(@"
                CREATE INDEX IX_AuditLog_UserId_Timestamp 
                    ON ""AuditLogs"" (""UserId"", ""Timestamp"");
            ");

            // Additional indexes for common queries
            migrationBuilder.Sql(@"
                CREATE INDEX IX_Products_IsActive_CategoryId 
                    ON ""Products"" (""IsActive"", ""CategoryId"");
            ");
            
            migrationBuilder.Sql(@"
                CREATE INDEX IX_Products_IsActive_ManufacturerId 
                    ON ""Products"" (""IsActive"", ""ManufacturerId"");
            ");
            
            migrationBuilder.Sql(@"
                CREATE INDEX IX_Products_MinStock_MaxStock 
                    ON ""Products"" (""MinStock"", ""MaxStock"") WHERE ""IsActive"" = true;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the indexes in reverse order
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Products_MinStock_MaxStock;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Products_IsActive_ManufacturerId;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Products_IsActive_CategoryId;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_AuditLog_UserId_Timestamp;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_AuditLog_EntityName_EntityId;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_InventoryTransaction_RequestId;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_InventoryTransaction_UserId_Date;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_InventoryTransaction_WarehouseId_Type;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_InventoryTransaction_Type_Date;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_InventoryTransaction_ProductId_Date;");
        }
    }
}
