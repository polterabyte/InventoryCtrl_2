using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Models;

public static class SqlViewInitializer
{
    public static async Task EnsureViewsAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        // vw_product_pending
        await db.Database.ExecuteSqlRawAsync(@"
CREATE OR REPLACE VIEW vw_product_pending AS
SELECT
  p.""Id""            AS product_id,
  p.""Name""          AS product_name,
  p.""SKU""           AS sku,
  COALESCE(SUM(t.""Quantity""), 0) AS pending_qty,
  MIN(t.""Date"") AS first_pending_date,
  MAX(t.""Date"") AS last_pending_date
FROM ""Products"" p
LEFT JOIN ""InventoryTransactions"" t ON t.""ProductId"" = p.""Id"" AND t.""Type"" = 3
GROUP BY p.""Id"", p.""Name"", p.""SKU"";
", cancellationToken);

        // vw_product_on_hand
        await db.Database.ExecuteSqlRawAsync(@"
CREATE OR REPLACE VIEW vw_product_on_hand AS
SELECT
  p.""Id"" AS product_id,
  p.""Name"" AS product_name,
  p.""SKU"" AS sku,
  COALESCE(SUM(
    CASE
      WHEN t.""Type"" = 0 THEN t.""Quantity""
      WHEN t.""Type"" IN (1,2) THEN -t.""Quantity""
      ELSE 0
    END
  ), 0) AS on_hand_qty
FROM ""Products"" p
LEFT JOIN ""InventoryTransactions"" t ON t.""ProductId"" = p.""Id""
GROUP BY p.""Id"", p.""Name"", p.""SKU"";
", cancellationToken);

        // vw_product_on_hand_by_wh
        await db.Database.ExecuteSqlRawAsync(@"
CREATE OR REPLACE VIEW vw_product_on_hand_by_wh AS
SELECT
  p.""Id"" AS product_id,
  p.""Name"" AS product_name,
  p.""SKU"" AS sku,
  w.""Id"" AS warehouse_id,
  w.""Name"" AS warehouse_name,
  COALESCE(SUM(
    CASE
      WHEN t.""Type"" = 0 THEN t.""Quantity""
      WHEN t.""Type"" IN (1,2) THEN -t.""Quantity""
      ELSE 0
    END
  ), 0) AS on_hand_qty
FROM ""Products"" p
JOIN ""InventoryTransactions"" t ON t.""ProductId"" = p.""Id""
JOIN ""Warehouses"" w ON w.""Id"" = t.""WarehouseId""
GROUP BY p.""Id"", p.""Name"", p.""SKU"", w.""Id"", w.""Name"";
", cancellationToken);

        // vw_product_installed
        await db.Database.ExecuteSqlRawAsync(@"
CREATE OR REPLACE VIEW vw_product_installed AS
SELECT
  p.""Id"" AS product_id,
  p.""Name"" AS product_name,
  p.""SKU"" AS sku,
  l.""Id"" AS location_id,
  l.""Name"" AS location_name,
  COALESCE(SUM(t.""Quantity""), 0) AS installed_qty,
  MIN(t.""Date"") AS first_install_date,
  MAX(t.""Date"") AS last_install_date
FROM ""InventoryTransactions"" t
JOIN ""Products"" p ON p.""Id"" = t.""ProductId""
LEFT JOIN ""Locations"" l ON l.""Id"" = t.""LocationId""
WHERE t.""Type"" = 2
GROUP BY p.""Id"", p.""Name"", p.""SKU"", l.""Id"", l.""Name"";
", cancellationToken);
    }
}


