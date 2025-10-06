using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.API.Migrations
{
    /// <inheritdoc />
    public partial class PopulateUserWarehouseData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create UserWarehouse records from existing InventoryTransaction data
            migrationBuilder.Sql(@"
                INSERT INTO ""UserWarehouses"" (""UserId"", ""WarehouseId"", ""AccessLevel"", ""IsDefault"", ""AssignedAt"", ""CreatedAt"")
                SELECT DISTINCT 
                    t.""UserId"",
                    t.""WarehouseId"",
                    'Full' as ""AccessLevel"",
                    false as ""IsDefault"",
                    MIN(t.""Date"") as ""AssignedAt"",
                    CURRENT_TIMESTAMP as ""CreatedAt""
                FROM ""InventoryTransactions"" t
                WHERE t.""UserId"" IS NOT NULL 
                AND t.""UserId"" != ''
                AND EXISTS (SELECT 1 FROM ""AspNetUsers"" u WHERE u.""Id"" = t.""UserId"")
                AND EXISTS (SELECT 1 FROM ""Warehouses"" w WHERE w.""Id"" = t.""WarehouseId"")
                GROUP BY t.""UserId"", t.""WarehouseId""
                ON CONFLICT (""UserId"", ""WarehouseId"") DO NOTHING;
            ");

            // Step 2: Set default warehouse for each user (warehouse with most transactions)
            migrationBuilder.Sql(@"
                UPDATE ""UserWarehouses"" 
                SET ""IsDefault"" = true
                WHERE (""UserId"", ""WarehouseId"") IN (
                    SELECT 
                        uw.""UserId"",
                        uw.""WarehouseId""
                    FROM ""UserWarehouses"" uw
                    INNER JOIN (
                        SELECT 
                            t.""UserId"",
                            t.""WarehouseId"",
                            COUNT(*) as transaction_count,
                            ROW_NUMBER() OVER (PARTITION BY t.""UserId"" ORDER BY COUNT(*) DESC, MIN(t.""Date"") ASC) as rn
                        FROM ""InventoryTransactions"" t
                        WHERE t.""UserId"" IS NOT NULL 
                        AND t.""UserId"" != ''
                        GROUP BY t.""UserId"", t.""WarehouseId""
                    ) ranked ON uw.""UserId"" = ranked.""UserId"" AND uw.""WarehouseId"" = ranked.""WarehouseId""
                    WHERE ranked.rn = 1
                );
            ");

            // Step 3: Ensure users without transaction history get assigned to the first active warehouse
            migrationBuilder.Sql(@"
                INSERT INTO ""UserWarehouses"" (""UserId"", ""WarehouseId"", ""AccessLevel"", ""IsDefault"", ""AssignedAt"", ""CreatedAt"")
                SELECT 
                    u.""Id"" as ""UserId"",
                    (SELECT ""Id"" FROM ""Warehouses"" WHERE ""IsActive"" = true ORDER BY ""CreatedAt"" LIMIT 1) as ""WarehouseId"",
                    'Full' as ""AccessLevel"",
                    true as ""IsDefault"",
                    CURRENT_TIMESTAMP as ""AssignedAt"",
                    CURRENT_TIMESTAMP as ""CreatedAt""
                FROM ""AspNetUsers"" u
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""UserWarehouses"" uw WHERE uw.""UserId"" = u.""Id""
                )
                AND EXISTS (SELECT 1 FROM ""Warehouses"" WHERE ""IsActive"" = true)
                ON CONFLICT (""UserId"", ""WarehouseId"") DO NOTHING;
            ");

            // Step 4: For admin users, assign them to all warehouses (if they don't already have assignments)
            migrationBuilder.Sql(@"
                INSERT INTO ""UserWarehouses"" (""UserId"", ""WarehouseId"", ""AccessLevel"", ""IsDefault"", ""AssignedAt"", ""CreatedAt"")
                SELECT 
                    u.""Id"" as ""UserId"",
                    w.""Id"" as ""WarehouseId"",
                    'Full' as ""AccessLevel"",
                    false as ""IsDefault"",
                    CURRENT_TIMESTAMP as ""AssignedAt"",
                    CURRENT_TIMESTAMP as ""CreatedAt""
                FROM ""AspNetUsers"" u
                CROSS JOIN ""Warehouses"" w
                WHERE u.""Role"" = 'Admin'
                AND w.""IsActive"" = true
                AND NOT EXISTS (
                    SELECT 1 FROM ""UserWarehouses"" uw 
                    WHERE uw.""UserId"" = u.""Id"" AND uw.""WarehouseId"" = w.""Id""
                )
                ON CONFLICT (""UserId"", ""WarehouseId"") DO NOTHING;
            ");

            // Step 5: Set a default warehouse for admin users if they don't have one
            migrationBuilder.Sql(@"
                UPDATE ""UserWarehouses"" 
                SET ""IsDefault"" = true
                WHERE ""UserId"" IN (
                    SELECT u.""Id"" 
                    FROM ""AspNetUsers"" u 
                    WHERE u.""Role"" = 'Admin'
                    AND NOT EXISTS (
                        SELECT 1 FROM ""UserWarehouses"" uw 
                        WHERE uw.""UserId"" = u.""Id"" AND uw.""IsDefault"" = true
                    )
                )
                AND ""WarehouseId"" = (
                    SELECT ""Id"" FROM ""Warehouses"" 
                    WHERE ""IsActive"" = true 
                    ORDER BY ""CreatedAt"" 
                    LIMIT 1
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove all UserWarehouse records created by this migration
            migrationBuilder.Sql(@"
                DELETE FROM ""UserWarehouses""
                WHERE ""CreatedAt"" >= (
                    SELECT MIN(""AssignedAt"") 
                    FROM ""UserWarehouses""
                );
            ");
        }
    }
}